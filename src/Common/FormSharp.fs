module FormSharp.Core

open System
#if FABLE_COMPILER
open Feliz
#endif

type HttpStatusCode =
  | HttpStatusCode of int
    
[<RequireQualifiedAccess>]
type HttpVerb =
  | Get
  | Post
  | Put
  | Patch
    
type HttpEndpoint<'formType> =
  { Url: string
    Verb: HttpVerb
    TokenProvider: unit -> string option
    Headers: (string * string) list
    // we have to supply a decoder for the response as if we use an auto decoder by the time that is set up then
    // Fable has erased the generic type information and it's not possible to inline far enough back up the call chain   
    ResponseDecoder: (string -> 'formType) option
  }
  with
    static member Default = { Url = "" ; Verb = HttpVerb.Get ; TokenProvider = (fun _ -> None) ; Headers = List.empty ; ResponseDecoder = None }
    static member WithGet url (decoder:string -> 'formType) = { HttpEndpoint<'formType>.Default with Url = url ; ResponseDecoder = Some decoder } |> Some
    static member WithPost url = { HttpEndpoint<'formType>.Default with Url = url ; Verb = HttpVerb.Post } |> Some
    static member WithPut url = { HttpEndpoint<'formType>.Default with Url = url ; Verb = HttpVerb.Put } |> Some
    static member WithPatch url = { HttpEndpoint<'formType>.Default with Url = url ; Verb = HttpVerb.Patch } |> Some

let inline createJsonDecoder<'responseType> () =
  #if FABLE_COMPILER
  let cachedDecoder = Thoth.Json.Decode.Auto.generateDecoderCached<'responseType>()
  (fun json -> Thoth.Json.Decode.unsafeFromString cachedDecoder json)
  #else
  let cachedDecoder = Thoth.Json.Net.Decode.Auto.generateDecoderCached<'responseType>()
  (fun json -> Thoth.Json.Net.Decode.unsafeFromString cachedDecoder json)
  #endif
    
[<RequireQualifiedAccess>]
type ValidationResult =
  | Error of string
  | Warning of string
  | Ok

[<RequireQualifiedAccess>]
type PropertyValidator =
  | TextValidator of (string -> ValidationResult)
  | IntValidator of (int -> ValidationResult)
  | DateTimeValidator of (DateTime -> ValidationResult)
with
  static member ($) (_, x:string-> ValidationResult) = PropertyValidator.TextValidator x
  static member ($) (_, x:int-> ValidationResult) = PropertyValidator.IntValidator x
  static member ($) (_, x:DateTime-> ValidationResult) = PropertyValidator.DateTimeValidator x
let inline (|PropertyValidator|) x = Unchecked.defaultof<PropertyValidator> $ x


[<RequireQualifiedAccess>]
type PropertyGetter<'formType> =
  | TextGetter of ('formType -> string)
  | IntGetter of ('formType -> int)
  | DateTimeGetter of ('formType -> DateTime)
with
  static member ($) (_, x:'formType -> string) = PropertyGetter.TextGetter x
  static member ($) (_, x:'formType -> int) = PropertyGetter.IntGetter x
  static member ($) (_, x:'formType-> DateTime) = PropertyGetter.DateTimeGetter x
let inline (|PropertyGetter|) x = Unchecked.defaultof<PropertyGetter<'formType>> $ x

[<RequireQualifiedAccess>]
type PropertySetter<'formType> =
  | TextSetter of ('formType -> string ->'formType)
  | IntSetter of ('formType -> int ->'formType)
  | DateTimeSetter of ('formType -> DateTime ->'formType)
with
  static member ($) (_, x:'formType -> string ->'formType) = PropertySetter.TextSetter x
  static member ($) (_, x:'formType -> int ->'formType) = PropertySetter.IntSetter x
  static member ($) (_, x:'formType-> DateTime ->'formType) = PropertySetter.DateTimeSetter x
let inline (|PropertySetter|) x = Unchecked.defaultof<PropertySetter<'formType>> $ x

type TableColumnProp =
  | Title of string

[<RequireQualifiedAccess>]
type PropertyValue =
  | Text of string
  | Int of int
  | Date of DateTime

type Collection<'collectionItemType> =
  | NoCollection
  | WithCollection of 'collectionItemType

// See my comments on Shims below
type IFormComponent<'formType> =
  #if FABLE_COMPILER
  abstract member render : 'formType -> ReactElement
  #else
  abstract member render : 'formType -> 'formType
  #endif
  abstract member children : IFormComponent<'formType> list
  abstract member validate : obj -> ValidationResult

[<RequireQualifiedAccess>]
type CollectionAction =
  | Update of int
  | Add
  | Delete of int

type DropdownItem<'dropdownValueType> =
  { Value: 'dropdownValueType
    Label: string
  }

type GroupProp<'formType> =
  | Title of string
  | Description of string
  | Children of IFormComponent<'formType> list
and TableProp<'formType, 'collectionItemType> =
  | Label of string
  | CollectionGetter of ('formType -> 'collectionItemType list)
  | CollectionSetter of (CollectionAction -> 'formType -> 'collectionItemType -> 'formType)
  | ItemInserter of ('collectionItemType -> 'formType)
  | Columns of (TableColumnProp list*IFormComponent<'collectionItemType>) list
  | AddButton of ('formType -> 'collectionItemType list -> 'collectionItemType)
  | DeleteButton
  | NoItemsMessage of string
and DropdownProp<'formType, 'dropdownValueType> =
  | Label of string
  | Items of ('dropdownValueType*string) list
  | Getter of ('formType -> 'dropdownValueType)
  | Setter of ('formType -> 'dropdownValueType -> 'formType)
  // we want this to be an option on the form options and not have the user have to specify it, though we dont need
  // this to be an option here this allows the same helpers to work in both places
  | HttpItems of HttpEndpoint<DropdownItem<'dropdownValueType> list> option
  | DropdownValidator of ('dropdownValueType -> ValidationResult)
  | AllowEmpty
and InputProp<'formType> =
  | Label of string
  | InputGetter of PropertyGetter<'formType>
  | InputSetter of PropertySetter<'formType>
  | InputValidator of PropertyValidator
  static member Getter<'formType>(getter:'formType->string) =
    InputProp.InputGetter (PropertyGetter<'formType>.TextGetter getter)
  static member Getter<'formType>(getter:'formType->int) =
    InputProp.InputGetter (PropertyGetter<'formType>.IntGetter getter)
  static member Getter<'formType>(getter:'formType->DateTime) =
    InputProp.InputGetter (PropertyGetter<'formType>.DateTimeGetter getter)
    
  static member Setter<'formType>(setter:'formType->string->'formType) =
    InputSetter (PropertySetter<'formType>.TextSetter setter)
  static member Setter<'formType>(setter:'formType->int->'formType) =
    InputSetter (PropertySetter<'formType>.IntSetter setter)
  static member Setter<'formType>(setter:'formType->DateTime->'formType) =
    InputSetter (PropertySetter<'formType>.DateTimeSetter setter)
  
  
module Shims =
  (*
  Differ controls require different generic parameters - specifically collections need both a form type and
  a collection type. Expressing this in DUs is difficult (and/or not possible). We'd want to say something like this:
  
  type FormComponent<'formType> =
    | TextInput of FormProps<'formType>
    | Table<'collectionItemType> of FormProps<'formType,'collectionItemType>
  
  The table isn't valid syntax. Tried numerous approaches to "hide" the 'collectionItemType but ultimately it always
  results in an escaping generic type or reliance on reflection features not available to Fable.
  
  I do want to explore a couple of different approaches to the renderer - perhaps use a type extension in each
  UI target, the current preprocessor approach I put in to try this is really quite grim.
  *)
  let private validateFormProps props model =
    let propertyValidator = props |> List.tryPick(function | InputProp.InputValidator v -> Some v | _ -> None)      
    let propertyGetter = props |> List.tryPick(function | InputProp.InputGetter getter -> Some getter | _ -> None)      
      
    match propertyValidator, propertyGetter with    
    | Some (PropertyValidator.TextValidator pv), Some (PropertyGetter.TextGetter pg) ->
      model |> pg |> pv
    | Some (PropertyValidator.IntValidator pv), Some (PropertyGetter.IntGetter pg) ->
      model |> pg |> pv
    | Some (PropertyValidator.DateTimeValidator pv), Some (PropertyGetter.DateTimeGetter pg) ->
      model |> pg |> pv
    | _ -> ValidationResult.Ok
  
  type TextInputShim<'formType> (props:InputProp<'formType> list) =
    #if FABLE_COMPILER
    static member val renderer = ((fun _ _ -> Html.div []):'formType -> InputProp<'formType> list -> ReactElement) with get, set
    #else
    static member val renderer = ((fun state _ -> state):'formType -> InputProp<'formType> list -> 'formType) with get, set
    #endif
    member val props = props
    interface IFormComponent<'formType> with
      member ti.render state = ti.props |> TextInputShim.renderer state
      member ti.children = []
      member ti.validate model = unbox<'formType> model |> validateFormProps ti.props
      //member ti.validate<'formValidationType> (model:'formValidationType) : ValidationResult = model |> validateFormProps ti.props
    
  type GroupShim<'formType> (props:GroupProp<'formType> list) =
    #if FABLE_COMPILER
    static member val renderer = ((fun _ _ -> Html.div []):'formType -> GroupProp<'formType> list -> ReactElement) with get, set
    #else
    static member val renderer = ((fun state _ -> state):'formType -> GroupProp<'formType> list -> 'formType) with get, set
    #endif
    member val props = props
    interface IFormComponent<'formType> with
      member ti.render state = GroupShim.renderer state ti.props
      member ti.children =
        ti.props
        |> List.tryPick(function | GroupProp.Children children -> Some children | _ -> None)        
        |> Option.defaultValue []
      member ti.validate _ = ValidationResult.Ok
    
  type DateInputShim<'formType> (props:InputProp<'formType> list) =
    #if FABLE_COMPILER
    static member val renderer = ((fun _ _ -> Html.div []):'formType -> InputProp<'formType> list -> ReactElement) with get, set
    #else
    static member val renderer = ((fun state _ -> state):'formType -> InputProp<'formType> list -> 'formType) with get, set
    #endif
    member val props = props
    interface IFormComponent<'formType> with
      member ti.render state = DateInputShim.renderer state ti.props
      member ti.children = []
      member ti.validate model = unbox<'formType> model |> validateFormProps ti.props
    
  type TableShim<'formType,'collectionItemType> (props:TableProp<'formType, 'collectionItemType> list) =
    #if FABLE_COMPILER
    static member val renderer = ((fun _ _ -> Html.div []):'formType -> TableProp<'formType, 'collectionItemType> list -> ReactElement) with get, set
    #else
    static member val renderer = ((fun state _ -> state):'formType -> TableProp<'formType, 'collectionItemType> list -> 'formType) with get, set
    #endif
    member val props = props
    interface IFormComponent<'formType> with
      member ti.render state = TableShim.renderer state ti.props
      member ti.children = []
      member ti.validate _ = ValidationResult.Ok
      
  type DropdownShim<'formType,'dropdownValueType> (props:DropdownProp<'formType, 'dropdownValueType> list) =
    #if FABLE_COMPILER
    static member val renderer = ((fun _ _ -> Html.div []):'formType -> DropdownProp<'formType, 'dropdownValueType> list -> ReactElement) with get, set
    #else
    static member val renderer = ((fun state _ -> state):'formType -> DropdownProp<'formType, 'dropdownValueType> list -> 'formType) with get, set
    #endif
    member val props = props
    interface IFormComponent<'formType> with
      member ti.render state = DropdownShim.renderer state ti.props
      member ti.children = []
      member ti.validate _ = ValidationResult.Ok

let TextInput<'formType> properties = Shims.TextInputShim properties :> IFormComponent<'formType>
let Group<'formType> properties = Shims.GroupShim properties :> IFormComponent<'formType>
let DateInput<'formType> properties = Shims.DateInputShim properties :> IFormComponent<'formType>
let Table<'formType, 'collectionItemType> (properties:TableProp<'formType, 'collectionItemType> list)
  = Shims.TableShim properties :> IFormComponent<'formType>
let Dropdown<'formType, 'dropdownValueType> (properties:DropdownProp<'formType, 'dropdownValueType> list)
  = Shims.DropdownShim properties :> IFormComponent<'formType>
       
let inline Validate (PropertyValidator x) = InputProp.InputValidator x
    
(*
type Property private () =
  static member getter<'formType>(getter:'formType->string) =
    InputProp.Getter (PropertyGetter<'formType>.TextGetter getter)
  static member getter<'formType>(getter:'formType->int) =
    InputProp.Getter (PropertyGetter<'formType>.IntGetter getter)
  static member getter<'formType>(getter:'formType->DateTime) =
    InputProp.Getter (PropertyGetter<'formType>.DateTimeGetter getter)
    
  static member setter<'formType>(setter:'formType->string->'formType) =
    Setter (PropertySetter<'formType>.TextSetter setter)
  static member setter<'formType>(setter:'formType->int->'formType) =
    Setter (PropertySetter<'formType>.IntSetter setter)
  static member setter<'formType>(setter:'formType->DateTime->'formType) =
    Setter (PropertySetter<'formType>.DateTimeSetter setter)
*)
    
type Collection private () =
  static member getter getter =
    TableProp.CollectionGetter getter
  static member setter setter =
    TableProp.CollectionSetter setter
        
[<RequireQualifiedAccess>]
type Button =
  | Save
  | Cancel
    
type FormOptions<'formType> =
  { // called then the save button is pressed and after a successful save
    OnComplete: 'formType -> unit
    // called when the cancel button is pressed
    OnCancel: 'formType -> unit
    // called when the state is changed
    OnChange: 'formType -> unit
    // the buttons to show
    Buttons: Button list
    // if not None then this will be called on load to obtain the state
    LoadFromUrl: HttpEndpoint<'formType> option
    // it not None then this will be called when save is pressed to save the state
    SaveToUrl: HttpEndpoint<'formType> option
    // if Some then will be called to get the latest token at the point of API call
    ApiTokenProvider: (unit -> string) option
    // If true then will show validation messages before form controls are dirty when appropriate
    ShowValidationWhenNotDirty: bool
  }
  with
    static member Default =
      { OnComplete = (fun _ -> ())
        OnCancel = fun _ -> ()
        OnChange = fun _ -> ()
        Buttons = [ Button.Save ]
        LoadFromUrl = None
        SaveToUrl = None
        ApiTokenProvider = None
        ShowValidationWhenNotDirty = false
      }:FormOptions<'formType>    


module Helpers =
  [<RequireQualifiedAccess>]
  type Value =
    | Text of string
    | Int of int
    | Date of DateTime
    with
      member x.AsString =
        match x with
        | Text txt -> txt
        | Int i -> i.ToString()
        | Date dt -> dt.ToString("yyyy-MM-dd")    
      
  let getInputValidator props = props |> List.tryPick(function | InputProp.InputValidator validator -> Some validator | _ -> None)    
  let getInputPropertyGetter props = props |> List.tryPick(function | InputProp.InputGetter getter -> Some getter | _ -> None)    
  let getInputPropertySetter props = props |> List.tryPick(function | InputProp.InputSetter setter -> Some setter | _ -> None)
  
  let getDropdownValidator props = props |> List.tryPick(function | DropdownProp.DropdownValidator validator -> Some validator | _ -> None)
  let getDropdownPropertyGetter props = props |> List.tryPick(function | DropdownProp.Getter getter -> Some getter | _ -> None)
  let getDropdownPropertySetter props = props |> List.tryPick(function | DropdownProp.Setter setter -> Some setter | _ -> None)
    
  let getInputValueAndValidationState props model =
    let validator = props |> getInputValidator          
    let propertyGetter = props |> getInputPropertyGetter
    
    match propertyGetter with
    | Some (PropertyGetter.TextGetter func) ->
      let value = func model
      let validationState = validator |> (function | Some (PropertyValidator.TextValidator tv) -> tv value | _ -> ValidationResult.Ok)
      Value.Text value, validationState
    | Some (PropertyGetter.IntGetter func) ->
      let value = func model
      let validationState = validator |> (function | Some (PropertyValidator.IntValidator tv) -> tv value | _ -> ValidationResult.Ok)
      Value.Int value, validationState
    | Some (PropertyGetter.DateTimeGetter func) ->
      let value = func model
      let validationState = validator |> (function | Some (PropertyValidator.DateTimeValidator tv) -> tv value | _ -> ValidationResult.Ok)
      Value.Date value, validationState
    | _ -> Value.Text "", ValidationResult.Ok
  
  // this will navigate the entire form and return the "worst" validation state - error being worst, ok being best
  // could certainly be optimised (could be said for this whole codebase right now!)
  let validateForm<'formType> (formDefinition:IFormComponent<'formType> list) (state:'formType) =
    let rec recursivelyValidate (formDefinition:IFormComponent<'formType> list) (state:'formType) =    
      formDefinition
      |> List.map(fun formComponent ->
        let componentValidationResult = formComponent.validate state
        let childrenValidationResult =
          recursivelyValidate formComponent.children state        
        componentValidationResult :: childrenValidationResult
      )
      |> List.concat
          
    recursivelyValidate formDefinition state 
    |> List.sortBy(function | ValidationResult.Ok -> 2 | ValidationResult.Warning _ -> 1 | ValidationResult.Error _ -> 0)
    |> List.head

  module State =  
    let updateList action item collection =
      match action with
      | CollectionAction.Add -> collection @ [item]
      | CollectionAction.Delete itemIndex ->
        (collection |> List.take itemIndex) @ (collection |> List.skip (itemIndex + 1))
      | CollectionAction.Update itemIndex ->
        collection |> List.mapi (fun i li -> if i=itemIndex then item else li)