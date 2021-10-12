// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Http
open Saturn
open Giraffe
open Model
open FSharp.Control.Tasks

type DropdownItem =
  { Value: Role
    Label: string
  }

let mutable personRepository = [
  { Id = Guid.Parse("0EB0F488-832F-4144-8492-0CFE73200347")
    Surname = "Smith"
    Forename = "Helen"
    DateOfBirth = DateTime(1991,10,12,0,0,0,DateTimeKind.Utc)
    Comments = []
    Role = Role.Shopper
  }
]

let getPerson (id:Guid) next ctx = task {
  // just a little delay so we can see the activity indicator on the clients
  //do! System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(2.))
  
  return!
    personRepository
    |> List.tryFind(fun p -> p.Id = id)
    |> Option.map (fun person -> json person next ctx )
    |> Option.defaultValue (Response.notFound ctx "Not found")
}

let createPerson next (ctx:HttpContext) = task {
  let! newPerson = ctx.BindJsonAsync<Person>()
  let personWithGeneratedId = { newPerson with Id = Guid.NewGuid() }
  personRepository <- personWithGeneratedId :: personRepository
  // just a little delay so we can see the activity indicator on the clients
  //do! System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(2.))
  return! json personWithGeneratedId next ctx
}

let updatePerson next (ctx:HttpContext) = task {
  let! updatedPerson = ctx.BindJsonAsync<Person>()
  personRepository <- personRepository |> List.map(fun p -> if p.Id = updatedPerson.Id then updatedPerson else p)
  Console.WriteLine(updatedPerson)
  // just a little delay so we can see the activity indicator on the clients
  //do! System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(2.))
  return! json updatedPerson next ctx
}

let getRoles next (ctx:HttpContext) = task {
  let items = [
    { Value = Role.Administrator ; Label = "Administrator" }
    { Value = Role.Shopper ; Label = "Shopper" }
  ]
  // just a little delay so we can see the activity indicator on the clients
  // do! System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(3.))
  return! json items next ctx
}

let apiRouter = router {
  get "/roles" getRoles
  getf "/person/%O" getPerson
  post "/person" createPerson
  put "/person" updatePerson
}  

let app = application {
  use_cors "cors_policy" (fun b -> b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader() |> ignore)
  use_json_serializer (Thoth.Json.Giraffe.ThothSerializer())
  use_router apiRouter
}

run app
