using UnityEngine;

public class BlockMazeManager : MonoBehaviour
{
    public BlockMazeGenerator mazeGenerator;
    public BlockMazeDecorator mazeDecorator;
    public bool generateOnStart = true; // 是否在游戏启动时自动生成迷宫

    private void Start()
    {
        // 如果开启了启动时自动生成，就自动生成迷宫
        if (generateOnStart)
        {
            GenerateCompleteMaze();
        }
    }

    public void GenerateCompleteMaze()
    {
        mazeGenerator.GenerateMaze();
        mazeDecorator.DecorateMaze();
        Debug.Log("方块迷宫生成和装饰完成！");
    }

    // 供UI按钮调用的方法，比如玩家点击"开始游戏"按钮后触发
    public void OnStartGameButtonClicked()
    {
        GenerateCompleteMaze();
    }

    [ContextMenu("Generate Block Maze")]
    public void GenerateMazeContextMenu()
    {
        GenerateCompleteMaze();
    }

    /// <summary>
    /// 重新生成迷宫并重置玩家位置（用于关卡切换、重开游戏）
    /// </summary>
    public void RestartGameWithNewMaze()
    {
        // 清除现有玩家
        GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
        if (existingPlayer != null)
        {
            Destroy(existingPlayer);
        }
        // 重新生成迷宫
        GenerateCompleteMaze();
    }
}