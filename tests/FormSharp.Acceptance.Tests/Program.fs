// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open System.Threading.Tasks
open Model
open FSharp.Control.Tasks
open Microsoft.Playwright
open Expecto
open FsHttp
open FsHttp.DslCE
open Hopac

let tests (browser:IBrowser) =
  let loadInitialPage () = task {
    let! page = browser.NewPageAsync()
    let! _ = page.GotoAsync("http://localhost:8080")
    let! loader = page.WaitForSelectorAsync(".loader", PageWaitForSelectorOptions(State=WaitForSelectorState.Attached))
    do! loader.WaitForElementStateAsync(ElementState.Hidden)
    return page
  }
  
  testList "Loading and updating" [
    testTask "Presents loaded content" {
      let! (result:string list) = task {
        let! page = loadInitialPage ()        
        let! surname = page.EvalOnSelectorAsync<string>("[name=\"input_1_0_Surname\"]", "e => e.value")
        let! forename = page.EvalOnSelectorAsync<string>("[name=\"input_1_1_Forename\"]", "e => e.value")
        let! dateOfBirth = page.EvalOnSelectorAsync<string>("[name=\"input_1_2_Date_of_birth\"]", "e => e.value")
        let! role = page.EvalOnSelectorAsync<string>("[name=\"input_1_3_Role\"]", "e => e.value")
        return [ surname ; forename ; dateOfBirth ; role ]
      }
      Expect.equal result.[0] "Smith" "Surname is incorrect"
      Expect.equal result.[1] "Helen" "Forename is incorrect"
      Expect.equal result.[2] "1991-10-12" "Date of birth is incorrect"
      Expect.equal result.[3] "1" "Role is incorrect"
    }
    
    testTask "Saves updated changes" {
      let! updatedPersonResult = task {
        let! page = loadInitialPage ()
        do! page.FillAsync("[name=\"input_1_0_Surname\"]", "Jane")
        do! page.FillAsync("[name=\"input_1_1_Forename\"]", "Bloggs")
        do! page.FillAsync("[name=\"input_1_2_Date_of_birth\"]", "1989-10-12")
        let! _ = page.SelectOptionAsync("[name=\"input_1_3_Role\"]", "0")        
        do! page.ClickAsync("button[type=\"submit\"]")
        //let! _ = page.WaitForSelectorAsync("button[type=\"submit\"]", PageWaitForSelectorOptions(State=WaitForSelectorState.Detached))
        let! _ = page.WaitForSelectorAsync("text=Thanks for updating your details!")
        let! _ = page.ScreenshotAsync(PageScreenshotOptions (Path="savescreenshot.png"))
        let! body = 
          http {
            GET "http://localhost:5000/person/0EB0F488-832F-4144-8492-0CFE73200347"
          }
          |> Response.toFormattedTextAsync
          |> Async.StartAsTask
        let person = Thoth.Json.Net.Decode.Auto.fromString<Person> body 
        return person
      }
      match updatedPersonResult with
      | Ok updatedPerson ->
        Expect.equal updatedPerson.Surname "Jane" "Surname is incorrect"
        Expect.equal updatedPerson.Forename "Bloggs" "Forename is incorrect"
        Expect.equal updatedPerson.DateOfBirth (DateTime(1989,10,12,0,0,0,DateTimeKind.Utc)) "Date of birth is incorrect"
        Expect.equal updatedPerson.Role Role.Administrator "Role is incorrect"
      | Error _ ->
        Expect.equal false true "Invalid response from server - should be Person model"
    }
  ]
    

[<EntryPoint>]
let main args =
  (task {
    use! playwright = Playwright.CreateAsync () 
    let! browser = playwright.Chromium.LaunchAsync()
    return runTestsWithCLIArgs [] args (tests browser)
  }).Result
  
  
  (*(task {
    use! playwright = Playwright.CreateAsync () 
    let! browser = playwright.Chromium.LaunchAsync()
    let! page = browser.NewPageAsync()
    let! _ = page.GotoAsync("http://localhost:8080")
    //let! _ = page.ScreenshotAsync(PageScreenshotOptions (Path = "screenshot.png" ))
    let! surname = page.EvalOnSelectorAsync<string>("[name=\"input_1_0_Surname\"]", "e => e.value")
    Console.WriteLine surname
    return 0
  }).Wait()*)
  //0 // return an integer exit code
  