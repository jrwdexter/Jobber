// Learn more about F# at http://fsharp.net. See the 'F# Tutorial' project
// for more guidance on F# programming.

#load "Jobber.fs"
#r "./packages/FSharp.Actor-logary/lib/net40/FSharp.Actor.dll"
#r "./packages/NodaTime/lib/portable-net4+sl5+netcore45+wpa81+wp8+MonoAndroid1+MonoTouch1+XamariniOS1/NodaTime.dll"
#r "./packages/NodaTime.Serialization.JsonNet/lib/portable-net4+sl5+netcore45+wpa81+wp8+MonoAndroid1+MonoTouch1/NodaTime.Serialization.JsonNet.dll"
#r "./packages/Newtonsoft.Json/lib/net40/Newtonsoft.Json.dll"
#r "./packages/Newtonsoft.Json.FSharp/lib/net40/Newtonsoft.Json.FSharp.dll"
#r "./packages/Logary/lib/net40/Logary.dll"

open Logary
open Logary.Metrics
open Logary.Targets
open Logary.Configuration
open NodaTime

let test _ =
    use logary =
        Config.withLogary' "Test" (
            Config.withTargets [Console.create Console.empty "console"] >>
//            Config.withMetrics (Duration.FromSeconds 4L) [
//                WinPerfCounters.create (WinPerfCounters.Common.cpuTimeConf) "cpuTime" (Duration.FromMilliseconds 500L)
//            ] >>
            Config.withRules [
                Rule.createForTarget "console"
            ] >>
            withInternalTargets Info [
                Console.create (Console.empty) "console"
            ]
        )

    let logger = Logary.Logging.getCurrentLogger ()
    LogLine.info "test" |> logger.Log

module TestCascade =
    let cascade _ =
        let logger = Logary.Logging.getCurrentLogger ()
        LogLine.info "test" |> LogLine.setPath "TestCascade" |> logger.Log
        
TestCascade.cascade ()

// Define your library scripting code here
