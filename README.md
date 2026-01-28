# 《觅长生》自动论道 Mod

这是一个用于《觅长生》游戏的自动论道 BepInEx Mod。

该 Mod 内置了多种论道策略，能够自动计算并推荐最优出牌决策。

## 功能特性

- 🎯 多种决策策略：支持贪心策略、前瞻策略、MCTS（蒙特卡洛树搜索）等
- 📊 实时评分展示：在卡牌上显示评分徽章
- 🔧 可扩展架构：策略与游戏逻辑分离，便于扩展

## 安装

1. 确保已安装 [BepInEx 5.x](https://github.com/BepInEx/BepInEx/releases)
2. 将 `AutoLunDao.dll` 与 `AutoLunDao.Core.dll` 放入 `BepInEx/plugins` 目录

## 构建项目

### 环境准备

1. 复制 `Directory.Build.props.template` 为 `Directory.Build.props.user`
2. 修改 `GamePath` 为你的游戏安装路径：

```xml

<Project>
    <PropertyGroup>
        <GamePath>你的游戏路径\觅长生</GamePath>
    </PropertyGroup>
</Project>
```

### 编译

```shell
dotnet build
```

## 项目结构

```
AutoLunDao/
├── Core/                   # 核心逻辑库（无游戏与 Unity 依赖）
│   ├── Entities/           # 数据实体（Card, State, Topic）
│   ├── Simulators/         # 论道出牌模拟器
│   └── Strategies/         # 决策策略
├── Engine/                 # 决策引擎
├── GameBridges/            # 游戏桥接层
├── UI/                     # UI 组件
├── Benchmarks/             # 策略基准测试
│   └── Sandboxes/          # 论道游戏沙盒
├── Plugin.cs               # BepInEx 插件入口
└── Patches.cs              # Harmony 补丁
```

## 策略说明

| 策略                       | 描述                |
|--------------------------|-------------------|
| BaselineStrategy         | 基线策略，模拟 NPC 的出牌策略 |
| ImprovedBaselineStrategy | 改进的基线策略           |
| GreedyStrategy           | 贪心策略              |
| LookaheadStrategy        | 前瞻策略              |
| MCTSStrategy             | 蒙特卡洛树搜索策略         |

## 开发

### 运行基准测试

```shell
cd Benchmarks
dotnet run --c Release
```

## 许可证
本项目采用 MIT 许可证，详情请参阅 [LICENSE](LICENSE) 文件。
