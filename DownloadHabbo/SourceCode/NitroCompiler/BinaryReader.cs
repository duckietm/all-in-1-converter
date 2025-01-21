using System;
using System.Text;

public class BinaryReader
{
    private int _position;
    private readonly byte[] _buffer;

    public BinaryReader(byte[] buffer)
    {
        _position = 0;
        _buffer = buffer;
    }

    public byte ReadByte()
    {
        byte value = _buffer[_position];
        _position++;
        return value;
    }

    public byte[] ReadBytes(int length)
    {
        byte[] data = new byte[length];
        Array.Copy(_buffer, _position, data, 0, length);
        _position += length;
        return data;
    }

    public short ReadShort()
    {
        byte[] bytes = ReadBytes(2);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return BitConverter.ToInt16(bytes, 0);
    }

    public int ReadInt()
    {
        byte[] bytes = ReadBytes(4);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return BitConverter.ToInt32(bytes, 0);
    }

    public int Remaining()
    {
        return _buffer.Length - _position;
    }

    public string ToString(Encoding encoding = null)
    {
        encoding ??= Encoding.UTF8;
        return encoding.GetString(_buffer, _position, Remaining());
    }

    public byte[] ToArrayBuffer()
    {
        return _buffer;
    }
}