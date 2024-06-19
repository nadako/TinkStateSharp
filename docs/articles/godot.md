# Godot Specifics

> **NOTE**: Godot support is currently fairly barebones and to be considered experimental. It seems to work pretty well, but it requires some amount of manual setup before using.

 * Build the TinkState project.
 * Find the `Nadako.TinkState.dll` and `Nadako.TinkState.Godot.dll` assemblies in the corresponding `projects/*/bin` subfolders.
 * Copy the assemblies somewhere in your project.
 * Add references to the assemblies to your Godot C# project
   <details>
   	<summary>csproj entries example (click me)</summary>

    ```xml
    <ItemGroup>
      <Reference Include="Nadako.TinkState.dll">
        <HintPath>Nadako.TinkState.dll</HintPath>
      </Reference>
      <Reference Include="Nadako.TinkState.Godot.dll">
        <HintPath>Nadako.TinkState.Godot.dll</HintPath>
      </Reference>
    </ItemGroup>
    ```
	</details>
 * Copy the `projects/TinkState-Godot/godot-script/TinkStateBatchScheduler.cs` script into your Godot project.
 * Add the `TinkStateBatchScheduler` script to your project's autoload list in `Project Settings->Autoload`.
