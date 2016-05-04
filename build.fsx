#I @"packages/FAKE/tools"
#I @"packages/FAKE.BuildLib/lib/net451"
#r "FakeLib.dll"
#r "BuildLib.dll"

open Fake
open Fake.Git.CommandHelper
open BuildLib

let solution = 
    initSolution
        "./ProjectScaffolding.sln" "Release"
        [ { emptyProject with Name = "ProjectScaffolding"
                              Folder = "./src/ProjectScaffolding" } ]

Target "Clean" <| fun _ -> cleanBin

Target "AssemblyInfo" <| fun _ -> generateAssemblyInfo solution

Target "Restore" <| fun _ -> restoreNugetPackages solution

Target "Build" <| fun _ ->
    buildSolution solution
    // pack UniGet.exe with dependent modules to packed one
    let ilrepackExe = (getNugetPackage "ILRepack" "2.0.9") @@ "tools" @@ "ILRepack.exe"
    Shell.Exec(ilrepackExe,
               "/wildcards /out:ProjectScaffolding.packed.exe ProjectScaffolding.exe *.dll",
               "./src/ProjectScaffolding/bin" @@ solution.Configuration) |> ignore

Target "PackZip" <| fun _ ->
    let workDir = binDir @@ "templates"
    ensureDirectory workDir
    runGitCommand "." ("archive --format=zip -o " + workDir + "/akka-unity.zip HEAD:templates/unity") |> ignore
    runGitCommand "." ("archive --format=zip -o " + workDir + "/akka-unity-cluster.zip HEAD:templates/unity-cluster") |> ignore
    let psExe = "./src/ProjectScaffolding/bin/Release/ProjectScaffolding.packed.exe"
    FileHelper.CopyFile (workDir @@ "akka-unity.exe") psExe
    FileHelper.CopyFile (workDir @@ "akka-unity-cluster.exe") psExe
    ZipHelper.Zip workDir (binDir @@ "Akka.ProjectScaffolding.zip") !!(workDir @@ "*")

Target "Pack" <| fun _ -> ()

Target "CI" <| fun _ -> ()

Target "Help" <| fun _ -> 
    showUsage solution (fun name -> 
        if name = "packzip" then Some("Pack all artifacts into a release zip", "")
        else None)

"Clean"
  ==> "AssemblyInfo"
  ==> "Restore"
  ==> "Build"

"Build" ==> "PackZip"
"PackZip" ==> "Pack"

RunTargetOrDefault "Help"
