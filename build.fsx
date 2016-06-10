#I @"packages/FAKE/tools"
#I @"packages/FAKE.BuildLib/lib/net451"
#r "FakeLib.dll"
#r "BuildLib.dll"

open Fake
open Fake.Git.CommandHelper
open Fake.Testing.XUnit2
open Fake.AppVeyor
open System
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
    for template in [ "unity"; "unity-cluster" ] do
        runGitCommand "." ("archive --format=zip -o " + workDir + "/akka-" + template + ".zip HEAD:templates/" + template) |> ignore
        FileHelper.CopyFile (workDir @@ ("akka-" + template + ".exe")) "./src/ProjectScaffolding/bin/Release/ProjectScaffolding.packed.exe"
    ZipHelper.Zip workDir (binDir @@ "Akka.ProjectScaffolding.zip") !!(workDir @@ "*")

Target "Pack" <| fun _ -> ()

Target "TestTemplates" <| fun _ ->
    ensureDirectory testDir
    let packTestDir = binDir @@ "packTest"
    ensureDirectory packTestDir
    let templateDir = binDir @@ "templates"
    for template in [ "unity"; "unity-cluster" ] do
        let result = ExecProcess (fun info ->
            info.FileName <- templateDir @@ ("/akka-" + template + ".exe")
            info.Arguments <- template + " -o \"" + packTestDir + "\"") TimeSpan.MaxValue
        if result <> 0 then failwithf ("Failed to run akka-")
        let solutionDir = packTestDir @@ template
        MSBuildRelease "" "Rebuild" [solutionDir @@ (template + ".sln")] |> ignore
        Fake.Testing.XUnit2.xUnit2 (fun p ->
            { p with ToolPath = xunitRunnerExe.Force()
                     ShadowCopy = false
                     XmlOutputPath = Some(testDir @@ template + ".xml") }) 
            [solutionDir @@ "src" @@ "Domain.Tests" @@ "bin" @@ "Release" @@ "Domain.Tests.dll";
             solutionDir @@ "src" @@ "GameServer.Tests" @@ "bin" @@ "Release" @@ "GameServer.Tests.dll"]
        if not (String.IsNullOrEmpty AppVeyorEnvironment.JobId) then UploadTestResultsFile Xunit (testDir @@ template + ".xml")

Target "CI" <| fun _ -> ()

Target "DevLink" <| fun _ ->
    let depDirs = [ "../Akka.Interfaced"; "../Akka.Interfaced.SlimSocket"; "../Akka.Cluster.Utility" ]
    devlink "./templates/unity/packages" depDirs
    devlink "./templates/unity-cluster/packages" depDirs
    
Target "Help" <| fun _ -> 
    showUsage solution (fun name -> 
        if name = "packzip" then Some("Pack all artifacts into a release zip", "")
        else None)

"Clean"
  ==> "AssemblyInfo"
  ==> "Restore"
  ==> "Build"

"Build" ==> "PackZip" ==> "TestTemplates"
"PackZip" ==> "Pack"

"TestTemplates" ==> "CI"
"Pack" ==> "CI"

RunTargetOrDefault "Help"
