// $begin{copyright}
//
// This file is part of WebSharper
//
// Copyright (c) 2008-2014 IntelliFactory
//
// Licensed under the Apache License, Version 2.0 (the "License"); you
// may not use this file except in compliance with the License.  You may
// obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
// implied.  See the License for the specific language governing
// permissions and limitations under the License.
//
// $end{copyright}

module WebSharper.Tests.Regression

open System
open WebSharper
open WebSharper.JavaScript
open WebSharper.Testing

[<Inline "new Array(0)">]
let private Empty<'T> : 'T [] = X

[<JavaScript>]
let private bug431_f ((x, y) as t) =
    [| y; x |]

[<JavaScript>]
let private bug431_g () =
    let t = (1, 2)
    bug431_f t

[<JavaScript>]
let private bug35_Foo (t: 'T * 'T) x = x

[<JavaScript>]
let private bug35_Bar () = bug35_Foo ("a","b") "c"

type FuncHelper =
    static member inline Compose f g x = g(f(x))

[<Proxy(typeof<FuncHelper>)>]
[<JavaScript>]
type FuncHelperProxy =
    [<Inline>]
    static member Compose f g x = g(f(x))

module private Bug61_M =

    type T[<JavaScript>]() =
        [<JavaScript>]
        member this.F = 1

type private Bug61_T2 [<JavaScript>]() =
    inherit Bug61_M.T()

module internal Bug367 =

    [<JavaScript>]
    let x = 1

    module B =
        [<JavaScript>]
        let y = x

    [<JavaScript>]
    let z = B.y

module BugBB80 =

    [<Sealed>]
    type X [<JavaScript>] (k: ref<int>) =
        interface System.IDisposable with
            [<JavaScript>]
            member this.Dispose() = incr k

    [<JavaScript>]
    let test () =
        async {
            let a = ref 0
            do! async {
                    use x = new X(a)
                    return ()
                }
            return Test "Bug BB80" { equal !a 1 }
        }
        |> Async.Start

module Bug264 =
    type Y<'X> = | Y of int

    type X =
        {
            A : int
            mutable B : Y<X>
        }

    [<JavaScript>]
    let Make (f: unit -> X) : Y<X> = Y (f().A)

module Bug323 =
    [<JavaScript>]
    type BaseClass(x) =
        member this.Value = x

    [<JavaScript>]    
    type DescendantClass(x) =
        inherit BaseClass(x + 1)

        member this.OriginalValue = x
                 
[<JavaScript>]
module Bug352 =
    [<AbstractClass>]
    type A() =
        abstract Foo : unit -> int
        default this.Foo() = 1

    type B() =
        inherit A()
        override this.Foo() = 2    

// TODO : does not translate yet 

//    [<JavaScript>]    
//    type BaseClass2 =
//        val x : int
//        new v = { x = v }
//        member this.Value = this.x
//
//    [<JavaScript>]    
//    type DescendantClass2 =
//        inherit BaseClass2
//
//        val x : int
//        new v = { inherit BaseClass(v + 1); x = v }
//        member this.OriginalValue = this.x

// for bug #328
type System.Object with
    [<Inline "$0.test">]
    member this.TestProperty = X<int>

    [<Inline "$0.test">]
    member this.TestMethod() = X<int>

    [<Inline "$0.testf($x)">]
    member this.TestMethod1 (x: int) = X<int[]>

    [<Inline "$0.testf($x, $y)">]
    member this.TestMethod2 (x: int, y: int) = X<int[]>

[<JavaScript>]
let Tests =
    TestCategory "Regression" {

        Test "Bug #26" {
            isTrueMsg ([||] = Empty<int>) "[||] = Empty<int>"
        }

        Test "Bug #35" {
            equal (bug35_Bar ()) "c"
        }

        Test "Bug #61" {
            let x = Bug61_T2()
            equal x.F 1
        }

        Test "Bug #367" {
            equal Bug367.B.y 1
            equal Bug367.z 1
        }

        Test "Bug #476" {
            let q = new System.Collections.Generic.Queue<int>()
            seq {
                q.Enqueue -1
                let c = ref 0
                while !c < 2 do
                    q.Enqueue !c
                    incr c
                q.Enqueue 2
                while !c < 4 do
                    q.Enqueue !c
                    incr c
                q.Enqueue 4
            }
            |> Seq.length |> ignore
            equal (q.ToArray()) [|-1; 0; 1; 2; 2; 3; 4|]
            let t (x: list<int>) = Seq.toArray (Seq.windowed 3 x)
            equal (t []) [||]
            equal (t [1]) [||]
            equal (t [1;2]) [||]
            equal (t [1;2;3]) [|[|1;2;3|]|]
            equal (t [1;2;3;4]) [|[|1;2;3|]; [|2;3;4|]|]
            equal (t [1;2;3;4;5]) [|[|1;2;3|]; [|2;3;4|]; [|3;4;5|]|]
        }

        Test "Bug #431" {
            equal (bug431_g()) [| 2; 1 |]
        }

        Test "Bug #484" {
            equal (string 0) "0"
        }

        do BugBB80.test()

        Test "Mutable" {
            equal (
                let mutable a = 2
                a <- 4
                a
            ) 4
        }

        Test "Bug #109" {
            let n = ResizeArray()
            do
                for i in 1 .. 10 do
                    for j in 1 .. 10 do
                        n.Add(fun k -> k + i + j)
            equal (Seq.sum [for x in n -> x 5]) 1600
        }

        Test "Bug #231" {
            let arr =
                [|
                    let prev = ref None
                    for c in [| 2; 2; 4; 2 |] do
                        match !prev with
                        | Some p ->
                            if p = c then
                                yield c * 2
                                prev := None
                            else
                                yield p
                                prev := Some c
                        | _ ->
                            prev := Some c
                    match !prev with
                    | Some p -> yield p
                    | _ -> ()
                |]
            equal arr [| 4; 4; 2 |]
        }

        Test "Bug #249" {
            equal (double 1) 1.0
        }

        Test "Bug #264" {
            let Mk () =
                let v =
                    {
                        A = 0
                        B = Unchecked.defaultof<_>
                    } : Bug264.X
                v.B <- Bug264.Make (fun () -> { v with A = 2 })
                v
        
            isTrue (
                try Mk().B = Bug264.Y 2
                with _ -> false)
        }

        Test "Bug #323" {
            let a = Bug323.DescendantClass(3)
            equal (a.OriginalValue) 3
            equal (a.Value) 4    
        }

        Test "Bug #328" {
            let o = 
                New [
                    "test" => 3
                    "testf" => FuncWithRest<int, int[]> id
                ]
            equal o.TestProperty 3
            equal (o.TestMethod()) 3
            equal (o.TestMethod1(1)) [| 1 |]
            equal (o.TestMethod2(1, 2)) [| 1; 2 |]
        }

        Test "Curried inlining" {
            let add1 x = x + 1
            let twice x = x * 2
            equal (FuncHelper.Compose add1 twice 0) 2
            let f = FuncHelper.Compose add1 twice 
            equal (f 1) 4
            equal (f 2) 6
        }

        Test "Bug #352" {
            equal (Bug352.B().Foo()) 2    
        }
                 

        Test "Bug #396" {
            let disposed = ref false
            let mySeq =
                seq {
                    try yield 0
                    finally disposed := true
                }
            do
                try for x in mySeq do
                        failwith ""
                with _ -> ()
            isTrue !disposed 
        }
    }