using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace OrbisGL
{
    public static class Extensions
    {
        /// <summary>
        /// Marshal an structure as raw data,
        /// The unmanaged data fields are allocated and their pointers is returned in the given enumerator for disposal
        /// </summary>
        /// <param name="Struct">The structure to copy to the stream</param>
        /// <param name="Output">The output stream for the structure raw data</param>
        /// <returns>Unmanaged Pointers Allocated</returns>
        public static IEnumerable<IntPtr> CopyTo<T>(this T Struct, Stream Output) where T : struct
        {
            foreach (var Addr in MarshalCopyTo(Struct, Output))
                yield return Addr;
        }
        internal static IEnumerable<IntPtr> MarshalCopyTo(object Struct, Stream Output)
        {
            var Writer = new BinaryWriter(Output, Encoding.UTF8);
            foreach (var Field in Struct.GetType().GetFields())
            {
                var Type = Field.FieldType;

                object Val = Field.GetValue(Struct);

                bool IsNull = Val is null;

                if (Type.IsNullable())
                    Type = Nullable.GetUnderlyingType(Type);

                var MarshalAs = Field.GetCustomAttribute<MarshalAsAttribute>();
                var FieldOffset = Field.GetCustomAttribute<FieldOffsetAttribute>();

                if (FieldOffset != null)
                {
                    var Offset = FieldOffset.Value;
                    if (Offset >= Output.Length)
                    {
                        Output.SetLength(Offset);
                    }

                    Output.Position = Offset;
                }

                if (Val is string && MarshalAs?.Value == UnmanagedType.LPStr)
                {
                    Type = typeof(IntPtr);
                    if (!IsNull)
                    {
                        var Data = Encoding.UTF8.GetBytes(((string)Val) + "\x0");
                        var Addr = Marshal.AllocHGlobal(Data.Length);
                        Marshal.Copy(Data, 0, Addr, Data.Length);

                        yield return Addr;

                        Val = Addr;
                    }
                    else
                    {
                        Val = IntPtr.Zero;
                    }
                }

                if (Val is string && MarshalAs?.Value == UnmanagedType.ByValTStr)
                {
                    if (!IsNull)
                    {
                        var Data = Encoding.UTF8.GetBytes(((string)Val) + "\x0");

                        Val = Data;
                        Type = typeof(byte[]);

                        var Size = MarshalAs.SizeConst;
                        MarshalAs = new MarshalAsAttribute(UnmanagedType.ByValArray);
                        MarshalAs.SizeConst = Size;
                    }
                    else
                    {
                        Type = typeof(IntPtr);
                        Val = IntPtr.Zero;
                    }
                }

                if (Type.IsStruct() && MarshalAs?.Value != UnmanagedType.LPStruct)
                {
                    foreach (var Addr in MarshalCopyTo(Val, Output))
                        yield return Addr;

                    continue;
                }

                if (Type.IsStruct() && MarshalAs?.Value == UnmanagedType.LPStruct)
                {
                    if (!IsNull)
                    {
                        using (var Stream = new MemoryStream())
                        {
                            foreach (var Addr in MarshalCopyTo(Val, Stream))
                                yield return Addr;

                            var Alloc = Marshal.AllocHGlobal((int)Stream.Length);
                            Marshal.Copy(Stream.ToArray(), 0, Alloc, (int)Stream.Length);

                            yield return Alloc;

                            Type = typeof(IntPtr);
                            Val = Alloc;
                        }
                    }
                    else
                    {
                        Type = typeof(IntPtr);
                        Val = IntPtr.Zero;
                    }
                }

                if (MarshalAs?.Value == UnmanagedType.LPArray)
                {
                    if (!IsNull)
                    {
                        Array Arr = (Array)Val;

                        Type = Type.GetElementType();

                        IntPtr Addr = IntPtr.Zero;

                        using (var Stream = new MemoryStream())
                        {
                            if (Arr is byte[] Data)
                            {
                                Stream.Write(Data, 0, Data.Length);
                            }
                            else
                            {
                                var Allocator = new BinaryWriter(Stream);

                                foreach (var Item in Arr)
                                {
                                    WriteField(Allocator, Type, Item);
                                }
                            }

                            Addr = Marshal.AllocHGlobal((int)Stream.Length);
                            Marshal.Copy(Stream.ToArray(), 0, Addr, (int)Stream.Length);
                        }
                        
                        yield return Addr;
                        
                        Type = typeof(IntPtr);
                        Val = Addr;
                    }
                    else
                    {
                        Type = typeof(IntPtr);
                        Val = IntPtr.Zero;
                    }
                }

                if (MarshalAs?.Value == UnmanagedType.ByValArray)
                {
                    var Count = MarshalAs.SizeConst;

                    Array Arr = (Array)Val;

                    Type = Type.GetElementType();

                    if (Arr != null)
                    {
                        foreach (var Item in Arr)
                        {
                            if (Count-- == 0)
                                break;

                            WriteField(Writer, Type, Item);
                        }
                    }

                    while (Count-- > 0)
                    {
                        WriteField(Writer, Type, Activator.CreateInstance(Type));
                    }
                    continue;
                }

                WriteField(Writer, Type, Val);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteField(BinaryWriter Writer, Type Type, object Val)
        {
            if (Type.IsEnum)
            {
                switch (Type.GetEnumUnderlyingType().Name)
                {
                    case nameof(SByte):
                        Type = typeof(SByte);
                        Val = (sbyte)Val;
                        break;
                    case nameof(Int16):
                        Type = typeof(Int16);
                        Val = (short)Val;
                        break;
                    case nameof(Int32):
                        Type = typeof(Int32);
                        Val = (int)Val;
                        break;
                    case nameof(Int64):
                        Type = typeof(Int64);
                        Val = (long)Val;
                        break;
                    case nameof(Byte):
                        Type = typeof(Byte);
                        Val = (byte)Val;
                        break;
                    case nameof(UInt16):
                        Type = typeof(UInt16);
                        Val = (ushort)Val;
                        break;
                    case nameof(UInt32):
                        Type = typeof(UInt32);
                        Val = (uint)Val;
                        break;
                    case nameof(UInt64):
                        Type = typeof(UInt64);
                        Val = (ulong)Val;
                        break;
                    default:
                        throw new Exception("Unexpected Enum Type");
                }
            }

            switch (Type.Name)
            {
                case nameof(UIntPtr):
                    Writer.Write(((UIntPtr)Val).ToUInt64());
                    break;
                case nameof(IntPtr):
                    Writer.Write(((IntPtr)Val).ToInt64());
                    break;
                case nameof(Int32):
                    Writer.Write((int)Val);
                    break;
                case nameof(UInt32):
                    Writer.Write((uint)Val);
                    break;
                case nameof(Int64):
                    Writer.Write((long)Val);
                    break;
                case nameof(UInt64):
                    Writer.Write((ulong)Val);
                    break;
                case nameof(Int16):
                    Writer.Write((short)Val);
                    break;
                case nameof(UInt16):
                    Writer.Write((ushort)Val);
                    break;
                case nameof(Byte):
                    Writer.Write((byte)Val);
                    break;
                case nameof(SByte):
                    Writer.Write((sbyte)Val);
                    break;
                default:
                    throw new Exception($"{Type.Name} isn't a supported field type");
            }
        }

        private static bool IsStruct(this Type T)
        {

            return T.IsValueType && !T.IsEnum && !T.IsPrimitive;
        }
        private static bool IsNullable(this Type T)
        {
            return Nullable.GetUnderlyingType(T) != null;
        }
    }
}