namespace FormSharp

open Feliz
open System
open FableCore
open Fable.Core.JS

[<AutoOpen>]
module Fable =
  [<AutoOpen>]
  module ReactExtensions =
    type React with
      [<Hook>]
      static member useForm<'formType>(initialState: 'formType,
                                       formDefinition: IFormComponent<'formType> list,
                                       renderer: string -> RendererState<'formType> -> (RendererState<'formType> -> unit) -> Button list -> bool -> (RendererState<'formType> -> Promise<unit>) -> (RendererState<'formType> -> Promise<unit>) -> IFormComponent<'formType> list -> Fable.React.ReactElement,
                                       formProps:FormProp<'formType> list) =
        let options = FormOptions<'formType>.FromProps formProps
        let state, setState = React.useState({
          Model = initialState
          ComponentsLoading = if options.LoadFromUrl |> Option.isSome then 1 else 0 
          IsSaving = false
          IsDisabled = false
          IsDirty = false
          ErrorMessage = None
        })
        let formId,_ = React.useState(Guid.NewGuid.ToString())
        
        let updateState state =
          setState state
          options.OnChange state.Model
        let saveFunc = createSaveFunction options setState
        let loadFunc = createLoadFunction options setState
        let composedRenderer = renderer formId state updateState options.Buttons options.ShowValidationWhenNotDirty saveFunc loadFunc
        
        Html.div (formDefinition |> composedRenderer)    



