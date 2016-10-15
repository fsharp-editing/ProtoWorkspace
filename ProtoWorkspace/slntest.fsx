System.IO.Directory.SetCurrentDirectory __SOURCE_DIRECTORY__
#load "SolutionSystem.fs"
open System.IO
open ProtoWorkspace

let testSlnPath = "../data/TestSln.sln"

let readSln filePath =
    use stream = File.OpenRead filePath
    use reader = new StreamReader(stream)
    SolutionFile.parse reader
;;

(readSln testSlnPath).ToString()
