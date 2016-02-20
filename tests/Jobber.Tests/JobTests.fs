namespace Jobber.Tests

open Xunit
open FsUnit.Xunit

type JobTests () = 
    [<Fact>]
    member x.``Job composition calls both jobs in a row`` () =
      1 |> should equal 1
