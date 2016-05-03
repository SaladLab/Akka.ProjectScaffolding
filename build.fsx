#I @"packages/FAKE/tools"
#I @"packages/FAKE.BuildLib/lib/net451"
#r "FakeLib.dll"
#r "BuildLib.dll"

open Fake
open BuildLib

let solution = 
    initSolution
        "./ProjectScaffolding.sln" "Release" 
        [ { emptyProject with Name = "ProjectScaffolding" 
                              Folder = "./src/ProjectScaffolding" } ]

Target "Clean" <| fun _ -> cleanBin

Target "AssemblyInfo" <| fun _ -> generateAssemblyInfo solution

Target "Restore" <| fun _ -> restoreNugetPackages solution

Target "Build" <| fun _ -> buildSolution solution

Target "PackNuget" <| fun _ -> createNugetPackages solution

Target "Pack" <| fun _ -> ()

Target "PublishNuget" <| fun _ -> publishNugetPackages solution

Target "Publish" <| fun _ -> ()

Target "CI" <| fun _ -> ()

Target "Help" <| fun _ -> 
    showUsage solution (fun _ -> None)

"Clean"
  ==> "AssemblyInfo"
  ==> "Restore"
  ==> "Build"

let isPublishOnly = getBuildParam "publishonly"

"Build" ==> "PackNuget" =?> ("PublishNuget", isPublishOnly = "")
"PackNuget" ==> "Pack"
"PublishNuget" ==> "Publish"

RunTargetOrDefault "Help"
