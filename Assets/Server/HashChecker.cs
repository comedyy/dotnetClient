using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class HashCompareItem
{
    public int hash;
    public int hashIndex;
    public int frame;
    public List<int> whos = new List<int>();
}

public enum FrameCheckErrorType
{
    None,
    HashError,
    FrameError,
}

public class HashChecker
{
    const int MAX_HASH_COUNT = 64;
    List<HashCompareItem> _allHashCompare;


    public HashChecker()
    {
        _allHashCompare = new List<HashCompareItem>();
        for(int i = 0; i < MAX_HASH_COUNT; i++)
        {
            _allHashCompare.Add(new HashCompareItem());
        }
    }

    public FrameCheckErrorType AddHash(FrameHash hash)
    {
        GuaranteeSize(hash);

        var currentHashIndex = hash.hashIndex;
        var lastIndex = _allHashCompare.Last().hashIndex;
        if(currentHashIndex > lastIndex)
        {
            Console.WriteLine("inner error");
            return FrameCheckErrorType.None;
        }

        var firstIndex = lastIndex - _allHashCompare.Count + 1;
        if(currentHashIndex < firstIndex) return FrameCheckErrorType.None;// 过期

        var diff = lastIndex - currentHashIndex;
        var compareIndex = _allHashCompare.Count - 1 - diff;
        var compare = _allHashCompare[compareIndex];

        if(compare.hash == 0)
        {
            compare.frame = hash.frame;
            compare.hash = hash.hash;
        }
        else
        {
            if(compare.hashIndex != hash.hashIndex)
            {
                // error
                Console.WriteLine($"error here hashIndex,  ${compare.hashIndex} != ${hash.hashIndex}");
                return FrameCheckErrorType.HashError;
            }

            if(compare.hash != hash.hash)
            {
                return FrameCheckErrorType.HashError;
            }

            if(compare.frame != hash.frame)
            {
                return FrameCheckErrorType.FrameError;
            }
        }

        return FrameCheckErrorType.None;
    }

    private void GuaranteeSize(FrameHash hash)
    {
        var currentHashIndex = hash.hashIndex;
        var lastIndex = _allHashCompare.Last().hashIndex;
        if(currentHashIndex <= lastIndex) return;

        var diff = currentHashIndex - lastIndex;
        for(int i = 0; i < diff; i++)
        {
            var x = _allHashCompare[0];
            _allHashCompare.RemoveAt(0);
            _allHashCompare.Add(x);
            x.hashIndex = i + 1 + lastIndex;
            x.frame = 0;
            x.hash = 0;
        }
    }
}