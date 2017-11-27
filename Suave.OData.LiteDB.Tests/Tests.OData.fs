module Tests.OData

open Expecto
open System.IO
open LiteDB
open LiteDB.FSharp
open Types
open Suave.Testing
open Suave
open Suave.OData.LiteDB
open Suave.OData.LiteDB.Json
open System.Net.Http
open Suave.Http
open System.Net
open System.Threading

let useDatabase (f: LiteRepository -> WebPart) = 
    let mapper = FSharpBsonMapper()
    let memoryStream = new MemoryStream()
    let db = new LiteRepository(memoryStream, mapper)
    f db
    
let odataRouter() = 
  useDatabase<| fun db->
    db.Insert({Id=0;Name="test"})|>ignore
    db.Database.GetCollection<Company>().Insert({Id=0;Name="Hello"})|>ignore
    resource "odata/company" (db.Database.GetCollection<Company>()) |> OData.CRUD
let runWithConfig = runWith defaultConfig
  
let ODataTests =
  testList "ODataTests" [
    testCase "OData GetById Test" <| fun _ -> 
      let ctx=runWithConfig <|odataRouter()
      let res=
       ctx
       |>req GET "odata/company(1)" None
       |>ofJson<Company>
      if not ctx.cts.IsCancellationRequested then 
         printfn "Dispose Context"
         disposeContext ctx
      else  printfn "NoNeed"
      Expect.equal res.Name  "test" "OData GetById Test Corrently" 
    testCase "OData Add Entity Test" <| fun _ -> 
      let ctx=runWithConfig <|odataRouter()
      let newCompany={Id=0;Name="newCompany"}|>toJson
      let data=new StringContent(newCompany)
      let res=
        ctx
        |>req POST "odata/company" (Some data)
        |>ofJson<Company>
      if not ctx.cts.IsCancellationRequested then 
         printfn "Dispose Context"
         disposeContext ctx
      else  printfn "NoNeed"
      Expect.equal res.Name  "newCompany" "OData Add Entity Test Corrently"  
    testCase "OData Delete Entity Test" <| fun _ -> 
      let ctx=runWithConfig <|odataRouter()
      let res=
        ctx
        |>req DELETE "odata/company(2)" None
        |>ofJson<Company>
      if not ctx.cts.IsCancellationRequested then 
         printfn "Dispose Context"
         disposeContext ctx      
      else  printfn "NoNeed"
      Expect.equal res.Name  "Hello" "OData Delete Entity Test Corrently"  
    testCase "OData Update Entity Test" <| fun _ -> 
      let ctx=runWithConfig <|odataRouter()

      let updatedCompany={Id=2;Name="updatedCompany"}|>toJson
      let data=new StringContent(updatedCompany)
      let res=
        ctx
          |>req PUT "odata/company(2)" (Some data)
          |>ofJson<Company>
      if not ctx.cts.IsCancellationRequested then 
         printfn "Dispose Context"
         disposeContext ctx      
      else  printfn "NoNeed"
      Expect.equal res.Name  "updatedCompany" "OData Update Entity Test Corrently" 
    testCase "OData Filter Entity Test" <| fun _ -> 
      let ctx=runWithConfig <|odataRouter()
      let res=
        ctx
          |>reqQuery GET "odata/company" "$select=Name"
          |>ofJson<list<string>>
      if not ctx.cts.IsCancellationRequested then 
         printfn "Dispose Context"
         disposeContext ctx      
      else  printfn "NoNeed"
      Expect.equal res ["test";"Hello"] "OData Filter EntityTest Corrently"             
  ]