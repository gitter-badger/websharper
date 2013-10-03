﻿// Copyright 2013 IntelliFactory
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

namespace IntelliFactory.WebSharper

module PathConventions =
    open System
    open System.IO
    open System.Reflection
    open System.Web

    type AssemblyId =
        {
            ShortName : string
        }

        static member Create(s: string) =
            AssemblyId.Create(AssemblyName(s))

        static member Create(a: Assembly) =
            AssemblyId.Create(a.GetName())

        static member Create(n: AssemblyName) =
            { ShortName = n.Name }

        static member Create(t: Type) =
            AssemblyId.Create(t.Assembly)

    type ResourceKind =
        | ContentResource
        | ScriptResource

        static member Content = ContentResource
        static member Script = ScriptResource

    type EmbeddedResource =
        {
            Id : AssemblyId
            Kind : ResourceKind
            Name : string
        }

        static member Create(kind, id, name) =
            {
                Id = id
                Kind = kind
                Name = name
            }

    [<Sealed>]
    type PathUtility(root: string, combine: string -> string -> string) =
        let ( ++ ) = combine
        let content = root ++  "Content" ++ "WebSharper"
        let scripts = root ++ "Scripts" ++ "WebSharper"

        member p.JavaScriptPath(a) =
            scripts ++ (a.ShortName + ".js")

        member p.MinifiedJavaScriptPath(a) =
            scripts ++ (a.ShortName + ".min.js")

        member p.TypeScriptDefinitionsPath(a) =
            scripts ++ (a.ShortName + ".d.ts")

        member p.EmbeddedPath(r) =
            match r.Kind with
            | ContentResource -> content ++ r.Id.ShortName ++ r.Name
            | ScriptResource -> scripts ++ r.Id.ShortName ++ r.Name

        static member FileSystem(root) =
            PathUtility(root, fun a b -> Path.Combine(a, b))

        static member VirtualPaths(root) =
            let root =
                match root with
                | "" | null -> "/"
                | _ -> root
            PathUtility(root, fun a b ->
                let a = VirtualPathUtility.AppendTrailingSlash(a)
                VirtualPathUtility.Combine(a, b))