(***hide***)

(*
#frontmatter
---
title: Getting started
category: datascience
authors: David Zimmer
index: 0
---
*)

(***condition:prepare***)
#r "nuget: Deedle, 2.5.0"
#r "nuget: FSharp.Stats, 0.4.3"
#r "nuget: Newtonsoft.Json, 13.0.1"
#r "nuget: Plotly.NET, 2.0.0-preview.16"
#r "nuget: FSharp.Data, 4.2.7"

(***condition:ipynb***)
#if IPYNB
#r "nuget: Deedle, 2.5.0"
#r "nuget: FSharp.Stats, 0.4.3"
#r "nuget: Newtonsoft.Json, 13.0.1"
#r "nuget: Plotly.NET, 2.0.0-preview.16"
#r "nuget: Plotly.NET.Interactive, 2.0.0-preview.16"
#r "nuget: FSharp.Data, 4.2.7"
#endif // IPYNB

(**
[![Binder]({{root}}images/badge-binder.svg)](https://mybinder.org/v2/gh/fslaborg/fslaborg.github.io/gh-pages?filepath=content/tutorials/{{fsdocs-source-basename}}.ipynb)&emsp;
[![Script]({{root}}images/badge-script.svg)]({{root}}content/tutorials/{{fsdocs-source-basename}}.fsx)&emsp;
[![Notebook]({{root}}images/badge-notebook.svg)]({{root}}content/tutorials/{{fsdocs-source-basename}}.ipynb)

# Getting started

Glad to see you here! Now that you found out and learned about FsLab, this section aims to illustrate how FsLab packages synergize and can be used to tackle
practical data science challenges. Note that every package used througout the tutorial has its own documentation so if you are interested in Deedle (link), FSharp.Stats or Plotly.Net feel free to take a deeper dive.

## Referencing packages

FsLab is a meant to be a project incubation space and can be thought of as a safe heaven for both, package developers and package users by providing guidelines and tutorials. Packages provided by the community can be used on their own, in combination with other FsLab packages but also in combination with any other .netstandard 2.0 compatible package. From F# 5.0 on packages can be referenced using the following notation:

```fsharp
// Packages hosted by the Fslab community
#r "nuget: Deedle"
#r "nuget: FSharp.Stats"
// third party .net packages 
#r "nuget: Plotly.NET, 2.0.0-preview.16"
#r "nuget: Plotly.NET.Interactive, 2.0.0-preview.12"
#r "nuget: FSharp.Data"
```

after referencing the packages one can access their namespaces and use provided functions. In the following example we will reference the
top level namespaces and then use a function provided by the FSharp.Stats package to calculate a factorial:
*)
open FSharp.Stats

let factorialOf3 = SpecialFunctions.Factorial.factorial 3

(*** condition: ipynb ***)
#if IPYNB
factorialOf3
#endif // IPYNB

(***include-value:factorialOf3***)

(**
## Data access
Equipped with these packages we are now ready to tackle promises made in the first paragraph: solving a practical data science problem. We will start by retrieving the data using the FSharp.Data package, subsequently we will use Deedle (link), a powerful data frame library that makes tabular data accessible by data frame programming. (Note that the chosen names give insight on their type, however thanks to FSharp being a strongly typed language and the we can at any time hower over single values to see the assigned type.)
*)
open FSharp.Data
open Deedle

// Retrieve data using the FSharp.Data package
let rawData = Http.RequestString @"https://raw.githubusercontent.com/dotnet/machinelearning/master/test/data/housing.txt"

// And create a data frame object using the ReadCsvString method provided by Deedle.
// Note: Of course you can directly provide the path to a local source.
let df = Frame.ReadCsvString(rawData,hasHeaders=true,separators="\t")

// Using the Print() method, we can use the Deedle pretty printer to have a look at the data set.
df.Print()

(*** include-output ***)

(**
## Data crunching
The data set of choice is the boston housing data set. As you can see from analyzing the printed output, it consists of 506 rows. Each row represents a house in the boston city area and each column encodes a feature/variable, such as the number of rooms per dwelling (RoomsPerDwelling), Median value of owner-occupied homes in $1000's (MedianHomeValue) and even variables indicating if the house is bordering river charles (CharlesRiver, value = 1) or not (CharlesRiver, value = 0). 

Lets say in our analysis we are only interested in the variables just described, furthermore we only want to keep rows where the value of the indicator variable is 0. We can use Deedle to easily create a new frame that fullfills our criteria. In this example we also cast the value of the column "CharlesRiver" to be of type bool, this illustrates how data frame programming can become typesafe using deedle.
*)

let housesNotAtRiver = 
    df
    |> Frame.sliceCols ["RoomsPerDwelling";"MedianHomeValue";"CharlesRiver"]
    |> Frame.filterRowValues (fun s -> s.GetAs<bool>("CharlesRiver") |> not ) 

//sprintf "The new frame does now contain: %i rows and %i columns" housesNotAtRiver.RowCount housesNotAtRiver.ColumnCount

housesNotAtRiver.Print()

(*** include-output ***)

(**
## Data exploration

Exploratory data analysis is an approach favored by many - to meet this demand we strongly advertise the use of Plotly.Net. The following snippet illustrates how we can access a column of a data frame and create an interactive chart in no time. Since we might want an idea of the distribution of the house prices a histogram can come in handy: 
*)
open Plotly.NET

// Note that we explicitly specify that we want to work with the values as floats. 
// Since the row identity is not needed anymore when plotting the distribution we can
// directly convert the collection to a FSharp Sequence. 
let pricesNotAtRiver : seq<float> = 
    housesNotAtRiver
    |> Frame.getCol "MedianHomeValue"
    |> Series.values
    
let h1 = 
    Chart.Histogram(pricesNotAtRiver)
    |> Chart.withXAxisStyle("median value of owner occupied homes in 1000s")
    |> Chart.withXAxisStyle("price distribution")

(*** condition: ipynb ***)
#if IPYNB
h1
#endif // IPYNB

(***hide***)
h1 |> GenericChart.toChartHTML
(***include-it-raw***)

(**
Since plotly charts are interactive they invite us to combine mutliple charts. Let repeat the filter step and see if houses that are located at the river show a similar distribution:
*)

let housesAtRiver = 
    df
    |> Frame.sliceCols ["RoomsPerDwelling";"MedianHomeValue";"CharlesRiver"]
    |> Frame.filterRowValues (fun s -> s.GetAs<bool>("CharlesRiver"))

let pricesAtRiver : seq<float> = 
    housesAtRiver
    |> Frame.getCol "MedianHomeValue"
    |> Series.values

let h2 =     
    [
    Chart.Histogram(pricesNotAtRiver)
    |> Chart.withTraceName "not at river"
    Chart.Histogram(pricesAtRiver)
    |> Chart.withTraceName "at river"
    ]
    |> Chart.combine
    |> Chart.withXAxisStyle("median value of owner occupied homes in 1000s")
    |> Chart.withXAxisStyle("Comparison of price distributions")

(***hide***)
h2 |> GenericChart.toChartHTML
(***include-it-raw***)

(**
The interactive chart allows us to compare the distributions directly. We can now reconstruct our own idea of the city of boston, the sampled area, just by looking at the data e.g.:

Assuming that the sampling process was homogenous while observing that there are much more houses sampled that are not located on the riverside could indicate that a spot on the river is a scarce commodity.
This could also be backed by analyzing the tails of the distribution: it seems that houses located at the river are given a head-start in their assigned value - the distribution of the riverside houses is truncated on the left. 

Suppose we would have a customer that wants two models, one to predict the prices of a house at the riverside and one that predicts the prices if this is not the case, then we can meet this demand by using FSharp.Stats in combination with Deedle. Of course we need a variable that is indicative of the house price, in this we will check if the number of rooms per dwelling correlates with the house value:
*)

let pricesAll :Series<int,float> = 
    df
    |> Frame.getCol "MedianHomeValue"

let roomsPerDwellingAll :Series<int,float> = 
    df
    |> Frame.getCol "RoomsPerDwelling"   

let correlation = 
    let tmpPrices,tmpRooms = 
        Series.zipInner pricesAll roomsPerDwellingAll    
        |> Series.values 
        |> Seq.unzip
    Correlation.Seq.pearson tmpPrices tmpRooms
                                              
(***include-value:correlation***)

(**
So indeed, the number of rooms per dwelling shows a positiv correlation with the house prices. With a pearson correlation of ~0.7 it does not explain the house prices completely - but this is nothing that really surprises us, as one of our hypothesis is that the location (e.g. riverside) does also have influence on the price -  however, it should be sufficient to create a linear model. 

So now we will use FSharp.Stats to build the two linear models ordered by the hypothetical customer. We start by defining a function that performs the fitting and plots the result:
*)

open Fitting.LinearRegression.OrdinaryLeastSquares

let predictPricesByRooms description data = 
    let pricesAll :Series<_,float> = 
        data
        |> Frame.getCol "MedianHomeValue"

    let roomsPerDwellingAll :Series<_,float> = 
        data
        |> Frame.getCol "RoomsPerDwelling"   

    let fit = 
        let tmpRooms, tmpPrices = 
            Series.zipInner roomsPerDwellingAll pricesAll    
            |> Series.sortBy fst
            |> Series.values 
            |> Seq.unzip
        let coeffs = Linear.Univariable.coefficient (vector tmpRooms) (vector tmpPrices)
        let model  = Linear.Univariable.fit coeffs 
        let predictedPrices = tmpRooms |> Seq.map model
        [
        Chart.Point(tmpRooms,tmpPrices)
        |> Chart.withTraceName (sprintf "%s: data" description )
        Chart.Line(tmpRooms,predictedPrices)
        |> Chart.withTraceName (sprintf "%s: coefficients: intercept:%f, slope:%f" description coeffs.[0] coeffs.[1])
        ]                                  
        |> Chart.combine
        |> Chart.withXAxisStyle("rooms per dwelling")
        |> Chart.withYAxisStyle("median value")
    fit   

(**
Afterwards, we can apply the function on our prepared datasets and have a look at the model and especially the model coefficients. 
*)
let modelVis = 
    [
    predictPricesByRooms "not at river" housesNotAtRiver
    predictPricesByRooms "at river" housesAtRiver
    ]
    |> Chart.combine
    |> Chart.withSize(1200.,700.)

(***hide***)
modelVis |> GenericChart.toChartHTML
(***include-it-raw***)


(**
Both models approximate the data in a reasonable way. When we inspect the coefficients, we see that the models only differ slightly in slope, but have an absolute offset of ~7.5. This observation complements the insights gained by the explorative data analysis approach using the histogram! 
*)
