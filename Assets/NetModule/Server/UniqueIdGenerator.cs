
using System;

public class UniqueIdGenerator
{
    int _from;
    int _to;
    int _current = 0;

    public UniqueIdGenerator(int from, int to)
    {
        _from = from;
        _to = to;
        _current = from;
    }

    public int GeneratorUniqueId(byte count)
    {
        var ret = _current + 1;
        _current += count;

        if(_current >= _to)
        {
            _current = _from;

            ret = _current + 1;
            _current += count;
        }

        return ret;
    }
}