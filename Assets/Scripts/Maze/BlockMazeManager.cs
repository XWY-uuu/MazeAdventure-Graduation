using UnityEngine;
using System; // 必须加这个命名空间，用于委托

public class BlockMazeManager : MonoBehaviour
{
    public BlockMazeGenerator mazeGenerator;
    public BlockMazeDecorator mazeDecorator;
    public bool generateOnStart = true; // 是否在游戏启动时自动生成迷宫

    [Header("关卡管理")]
    public int currentLevel = 1;

    // ========== 新增：迷宫生成完成的事件委托 ==========
    public event Action OnMazeGenerated;

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
        // ========== 新增：触发事件，通知敌人刷新、导航烘焙 ==========
        OnMazeGenerated?.Invoke();
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
        GenerateCompleteMaze();
    }
}