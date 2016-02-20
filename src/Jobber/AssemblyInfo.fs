namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("Jobber")>]
[<assembly: AssemblyProductAttribute("Jobber")>]
[<assembly: AssemblyDescriptionAttribute("A library for processing jobs, which allows a succinct system for combining complex pipelines and tasks into a centrally managed framework.")>]
[<assembly: AssemblyVersionAttribute("0.0.1")>]
[<assembly: AssemblyFileVersionAttribute("0.0.1")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.0.1"
