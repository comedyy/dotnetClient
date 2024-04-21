using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SyncCode
{
    [MenuItem("Tools/sync")]
    public static void Sync()
    {
        var dest = @"/Users/zhangdunyong/work/matchserver/lockStepTest/Server";
        var from = @"/Users/zhangdunyong/work/test/dotnetClient/Assets/Server";

        FileUtil.DeleteFileOrDirectory(dest);
        FileUtil.CopyFileOrDirectory(from, dest);
    }
}
