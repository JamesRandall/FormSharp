# FormSharp

**This is very much work in progress - here be killer bunnies, but they're quickly being pacified**

![Alt text](./images/bunny.jpg?raw=true "Killer bunny")

FormSharp aims to take the drudgery out of creating data entry forms in F# by taking care of all the plumbing and presentation and hiding it behind a DSL. I've found myself in a couple of situations where I've had to build broad data entry systems and writing and maintaining this code manually will kill a small team.

The aim is to allow forms to be defined using a fairly simple and abstract DSL:

![Alt text](./images/dsl.png?raw=true "DSL")

That can then be executed in different runtimes / environments using an appropriate renderer / executer:

![Alt text](./images/reactRuntime.png?raw=true "React usage")

Keeping a few goals in mind as I work on it:

1. Support for multiple runtimes initially React, plain JavaScript and Spectre (console).
2. Support for multiple and custom renderers.
3. Easy to use - its supposed to save effort, not create effort.

The code is emerging from a production system and a poc and so is of varying quality and is definitely subject to significant change.

If you want to take a look at how it works best place to start is with the [Fable.React.Tailwind.Sample app]()https://github.com/jamesrandall/formSharp/tree/samples/Fable.React.Tailwind.Sample).

Docs to follow as I build it out.

I was really tempted to call it Phorm but their are some Phorm-esque things in PHP land.

## Things to do

In a rough order.

* Automated tests - wip, using PlayWright
* GitHub Action for build, test and package release
* Token injection for API calls (skeleton is their)
* Add support for loading, saving etc. from Fable Remoting using friendly syntax
* Transformers at point of load and save
* Additional components - check box, radio buttons, text areas
* Support injection of custom components in forms
* Add a Bootstrap renderer to the React package
* Add a vanilla HTML (no React) package (Tailwind and Bootstrap)
* Bring the Spectre support into the public repo from my poc

## Request for features, help and bug fixes

Sure. Go ahead. Issues and discussions are open. But I may ask for money in return. Releasing OSS doesn't come with any obligation and although some people seem to live in a world where money isn't needed most of us have a mortgage and bills to pay.

## Contributing

Best to speak to me first as I'm changing things a lot at the moment.

## License

MIT - see the LICENSE file.
