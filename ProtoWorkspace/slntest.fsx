System.IO.Directory.SetCurrentDirectory __SOURCE_DIRECTORY__

#load "scripts/load-references-release.fsx"
#load "SolutionSystem.fs"
open System.IO
open ProtoWorkspace

let testSlnPath = "../data/TestSln.sln"


(SolutionFile.load testSlnPath).ToString()
