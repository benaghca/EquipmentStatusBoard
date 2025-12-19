# Adjutant - Equipment Status Tracker

A WPF application for tracking and visualizing equipment status in data centers, mechanical rooms, and industrial facilities. Built with .NET 8 and MVVM architecture.

## Features

- Visual canvas for equipment layout with pan/zoom
- Equipment types: Valves, Breakers, Pumps, Generators, Transformers, UPS, ATS, and more
- Electrical and pipe connections with power flow visualization
- Real-time status tracking (Normal/Abnormal/Warning)
- LOTO (Lock Out Tag Out) support
- Undo/Redo functionality
- Layer management
- CSV export for equipment status and history
- Auto-save

## Architecture

```mermaid
graph TB
    subgraph Views
        MW[MainWindow]
        MW_I[MainWindow.Input.cs]
        MW_C[MainWindow.Canvas.cs]
    end

    subgraph ViewModels
        MVM[MainViewModel]
        CVM[CanvasViewModel]
        SVM[SelectionViewModel]
        TVM[ToolViewModel]
    end

    subgraph Services
        IPS[IProjectService]
        IUR[IUndoRedoService]
        PS[ProjectService]
        UR[UndoRedoService]
    end

    subgraph Models
        EQ[Equipment]
        CONN[Connection]
        PD[ProjectData]
        LY[Layer]
    end

    MW --> MVM
    MVM --> CVM
    MVM --> SVM
    MVM --> TVM
    MVM --> IPS
    MVM --> IUR
    IPS -.-> PS
    IUR -.-> UR
    MVM --> EQ
    MVM --> CONN
```

## ViewModel Composition

```mermaid
classDiagram
    class MainViewModel {
        +CanvasViewModel Canvas
        +SelectionViewModel Selection
        +ToolViewModel Tool
        +ObservableCollection~Equipment~ EquipmentCollection
        +ObservableCollection~Connection~ Connections
        +RecalculateElectricalStatus()
    }

    class CanvasViewModel {
        +double ZoomLevel
        +double PanOffsetX
        +double PanOffsetY
        +bool SnapToGrid
        +int GridSize
    }

    class SelectionViewModel {
        +IReadOnlyList~Equipment~ SelectedItems
        +AddToSelection()
        +ClearEquipmentSelection()
        +SelectInRect()
    }

    class ToolViewModel {
        +string SelectedTool
        +string SelectedConnectionTool
        +bool IsEditMode
        +HandleConnectionClick()
    }

    MainViewModel *-- CanvasViewModel
    MainViewModel *-- SelectionViewModel
    MainViewModel *-- ToolViewModel
```

## Electrical Power Flow

Power propagates from sources (Generators, Transformers) through conducting equipment:

```mermaid
flowchart LR
    subgraph Power Sources
        GEN[Generator\n'on']
        XFMR[Transformer\n'energized']
    end

    subgraph Conductors
        BRK[Breaker\n'closed']
        SW[Switch\n'closed']
        ATS[ATS\n'normal'/'emergency']
        UPS[UPS\n'on'/'bypass']
        PDU[PDU\n'on']
        BUS[BusBar\n'energized']
        JCT[Junction\nalways]
    end

    subgraph Loads
        PUMP[Pump]
        MOTOR[Motor]
        CHILLER[Chiller]
    end

    GEN -->|power| BRK
    XFMR -->|power| BRK
    BRK -->|conducts| ATS
    ATS -->|conducts| UPS
    UPS -->|conducts| PDU
    PDU -->|energizes| PUMP
    PDU -->|energizes| MOTOR
```

## Building

```bash
# Build
dotnet build

# Run tests
dotnet test

# Publish self-contained executable
dotnet publish EquipmentStatusTracker.WPF.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

## Requirements

- Windows 10 or later (64-bit)
- No .NET installation required for published executable

## License

MIT
