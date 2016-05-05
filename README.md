# Akka.ProjectScaffolding

A project generator which creates C# projects using Akka.NET and Unity3D.

### How to use

- Download the latest `Akka.ProjectScaffolding.zip` from [releases page](https://github.com/SaladLab/Akka.ProjectScaffolding/releases).
- Unzip `Akka.ProjectScaffolding.zip`
- Run `akka-unity` or `akka-unity-cluster` to generate new project.

For example, following command creates `NewProject` using akka-unity template
and locate it at %HOMEPATH%.

```
> akka-unity NewProject -o %HOMEPATH%
```

### Templates

Two templates are provided for helping creating new projects.

#### Template: akka-unity

- Server Configuration
  - Simple standalone server.
  - Console application using .NET 4.6 Framework.
  - Using Akka.NET, Akka.Interfaced and Akka.Interfaced.SlimSocket.
- Client Configuration
  - Unity3D application.
  - Using Akka.Interfaced.SlimSocket.

#### Template: akka-unity-cluster

- Server Configuration
  - Clustered server.
  - Console application using .NET 4.6 Framework.
  - Using Akka.NET, Akka.Interfaced, Akka.Interfaced.SlimSocket and Akka.Cluster.Utility.
- Client Configuration
  - Unity3D application.
  - Using Akka.Interfaced.SlimSocket.

### Project configuration

- Domain
  - Consists of actor interface and shared data.
- Domain.Tests
  - UnitTest for `Domain`
- Domain.Unity3D
  - Unity3D project of `Domain`
  - When files are added or deleted in `Domain`, do same for this.
    - For adding files, you need to use
      [Add as link](https://msdn.microsoft.com/en-us/library/windows/apps/jj714082.aspx).
  - Files which are not necessary for client in `Domain` can be omitted.
- GameClient
  - Unity3D GameClient
- GameServer
  - Console GameServer
- GameServer.Tests
  - UnitTest for `GameServer`
