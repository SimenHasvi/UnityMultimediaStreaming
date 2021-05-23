using JetBrains.Annotations;

public static class LocalTopic
{
    private static byte[] _data = new byte[0];
    private static bool _updated = false;

    public static void Produce(byte[] data)
    {
        _data = data;
        _updated = true;
    }

    [CanBeNull]
    public static byte[] Consume()
    {
        _data = _updated ? _data : null;
        _updated = false;
        return _data;
    }
}
