(***hide***)

(*
#frontmatter
---
title: Testing with FSharp.Stats I: t-test
category: datascience
authors: Oliver Maus
index: 5
---
*)

(***condition:prepare***)
#r "nuget: Deedle, 2.5.0"
#r "nuget: FSharp.Stats, 0.4.3"
#r "nuget: Newtonsoft.Json, 13.0.1"
#r "nuget: DynamicObj, 0.2.0"
#r "nuget: Plotly.NET, 2.0.0-preview.16"
#r "nuget: FSharp.Data, 4.2.7"

(***condition:ipynb***)
#if IPYNB
#r "nuget: Deedle, 2.5.0"
#r "nuget: FSharp.Stats, 0.4.3"
#r "nuget: Newtonsoft.Json, 13.0.1"
#r "nuget: DynamicObj, 0.2.0"
#r "nuget: Plotly.NET, 2.0.0-preview.16"
#r "nuget: Plotly.NET.Interactive, 2.0.0-preview.16"
#r "nuget: FSharp.Data, 4.2.7"
#endif // IPYNB



(**

[![Binder]({{root}}images/badge-binder.svg)](https://mybinder.org/v2/gh/fslaborg/fslaborg.github.io/gh-pages?filepath=content/tutorials/{{fsdocs-source-basename}}.ipynb)&emsp;
[![Script]({{root}}images/badge-script.svg)]({{root}}content/tutorials/{{fsdocs-source-basename}}.fsx)&emsp;
[![Notebook]({{root}}images/badge-notebook.svg)]({{root}}content/tutorials/{{fsdocs-source-basename}}.ipynb)

# Testing with FSharp.Stats I: t-test

## Getting started: The t-test

_I love statistical testing_ - A sentence math teachers don't hear often during their time at school. In this tutorial we aim to give you a short introduction of the theory and how to 
perform the most used statistical test: the t-test

Suppose you have measured the length of some leaves of two trees and you want to find out if the average length of the leaves is the same or if they differ from each other. 
If you knew the population distributions of all leaves hanging on both trees the task would be easy, but since we only have samples from both populations, we have to apply a statistical test.
Student's t-test can be applied to test whether two samples have the same mean (H0), or if the means are different (H1). There are two requirements to the samples that have to be fulfilled:

1. The variances of both samples have to be equal.

2. The samples have to follow a normal distribution.

_Note: Slight deviations from these requirements can be accepted but strong violations result in an inflated false positive rate. If the variances are not equal a Welch test can be performed._
_There are some tests out there to check if the variances are equal or if the sample follows a normal distribution, but their effectiveness is discussed._
_You always should consider the shape of the theoretical background distribution, instead of relying on preliminary tests rashly._


The t-test is one of the most used statistical tests in datascience. It is used to compare two samples in terms of statistical significance. 
Often a significance threshold (or &alpha; level) of 0.05 is chosen to define if a p value is defined as statistically significant. A p value describes how likely it is to observe an effect
at least as extreme as you observed (in the comparison) by chance. Low p values indicate a high confidence to state that there is a real difference and the observed difference is not caused by chance.

*)

#r "nuget: FSharp.Data"
#r "nuget: Deedle"
#r "nuget: FSharp.Stats, 0.4.2"
#r "nuget: Plotly.NET, 2.0.0-preview.16"

open FSharp.Data
open Deedle
open Plotly.NET

(**

For our purposes, we will use the housefly wing length dataset (from _Sokal et al., 1955, A morphometric analysis of DDT-resistant and non-resistant housefly strains_).
Head over to the [Getting started](001_getting-started.html#Data-access) tutorial where it is shown how to import datasets in a simple way.


*)

// We retrieve the dataset via FSharp.Data:
let rawDataHousefly = Http.RequestString @"https://raw.githubusercontent.com/fslaborg/datasets/main/data/HouseflyWingLength.txt"

let dataHousefly : seq<float> = 
    Frame.ReadCsvString(rawDataHousefly, false, schema = "wing length (mm * 10^1)")
    |> Frame.getCol "wing length (mm * 10^1)"
    |> Series.values
    // We convert the values to mm
    |> Seq.map (fun x -> x / 10.)

(**

Let us first have a look at the sample data with help of a boxplot. As shown below, the average wingspan is around 4.5 with variability ranges between 3.5 and 5.5.


*)

let boxPlot = 
    Chart.BoxPlot(y = dataHousefly, Name = "housefly", BoxPoints = StyleParam.BoxPoints.All, Jitter = 0.2)
    |> Chart.withYAxisStyle "wing length [mm]"

(*** condition: ipynb ***)
#if IPYNB
boxPlot
#endif // IPYNB

(***hide***)
boxPlot |> GenericChart.toChartHTML
(***include-it-raw***)


(**

## One-sample t-test

We want to analyze if an estimated expected value differs from the sample above. Therefore, we perform a one-sample t-test which covers exactly this situation.



<img style="max-width:75%" src="../../images/OneSampleTTest.png"></img>

Fig. 1: **The one-sample t-test** The dashed orange line depicts the distribution of our sample, the green bar the expected value to test against.

*)

open FSharp.Stats
open FSharp.Stats.Testing

// The testing module in FSharp.Stats require vectors as input types, thus we transform our array into a vector:
let vectorDataHousefly = vector dataHousefly

// The expected value of our population.
let expectedValue = 4.5

// Perform the one-sample t-test with our vectorized data and our exptected value as parameters.
let oneSampleResult = TTest.oneSample vectorDataHousefly expectedValue

(*** hide ***)

(*** include-value:oneSampleResult ***)

(**

The function returns a `TTestStatistics` type. If contains the fields 

  - `Statistic`: defines the exact teststatistic

  - `DegreesOfFreedom`: defines the degrees of freedom

  - `PValueLeft`: the left-tailed p-value 

  - `PValueRight`: the right-tailed p-value

  - `PValue`: the two-tailed p-value

As we can see, when looking at the two-tailed p-value, our sample does _not_ differ significantly from our expected value. This matches our visual impression of the boxplot, where the sample distribution 
is centered around 4.5.


## Two-sample t-test (unpaired data)

The t-test is most often used in its two-sample variant. Here, two samples, independent from each other, are compared. It is required that both samples are normally distributed.
In this next example, we are going to see if the gender of college athletes determines the number of concussions suffered over 3 years (from: _Covassin et al., 2003, Sex Differences and the Incidence of Concussions Among Collegiate Athletes, Journal of Athletic Training_).


<img style="max-width:75%" src="../../images/TwoSampleTTest.png"></img>

Fig. 2: **The two-sample t-test** The dashed orange and green lines depict the distribution of both samples that are compared with each other.

*)

open System.Text

let rawDataAthletes = Http.RequestString @"https://raw.githubusercontent.com/fslaborg/datasets/main/data/ConcussionsInMaleAndFemaleCollegeAthletes_adapted.tsv"

let dataAthletesAsStream = new System.IO.MemoryStream(rawDataAthletes |> Encoding.UTF8.GetBytes)

// The schema helps us setting column keys.
let dataAthletesAsFrame = Frame.ReadCsv(dataAthletesAsStream, hasHeaders = false, separators = "\t", schema = "Gender, Sports, Year, Concussion, Count")

dataAthletesAsFrame.Print()

// We need to filter out the columns and rows we don't need. Thus, we filter out the rows where the athletes suffered no concussions  
// as well as filter out the columns without the number of concussions.
let dataAthletesFemale, dataAthletesMale =
    let getAthleteGenderData gender =
        let dataAthletesOnlyConcussion =
            dataAthletesAsFrame
            |> Frame.filterRows (fun r objS -> objS.GetAs "Concussion")
        let dataAthletesGenderFrame =
            dataAthletesOnlyConcussion
            |> Frame.filterRows (fun r objS -> objS.GetAs "Gender" = gender)
        dataAthletesGenderFrame
        |> Frame.getCol "Count" 
        |> Series.values
        |> vector
    getAthleteGenderData "Female", getAthleteGenderData "Male"
    
(**

Again, let's check our data via boxplots before we proceed on comparing them.

*)

let boxPlot2 = 
    [
        Chart.BoxPlot(y = dataAthletesFemale, Name = "female college athletes", BoxPoints = StyleParam.BoxPoints.All, Jitter = 0.2)
        Chart.BoxPlot(y = dataAthletesMale, Name = "male college athletes", BoxPoints = StyleParam.BoxPoints.All, Jitter = 0.2)
    ]
    |> Chart.combine
    |> Chart.withYAxisStyle "number of concussions over 3 years"


(*** condition: ipynb ***)
#if IPYNB
boxPlot2
#endif // IPYNB

(***hide***)
boxPlot2 |> GenericChart.toChartHTML
(***include-it-raw***)

(**

Both samples are tested against using `FSharp.Stats.Testing.TTest.twoSample` and assuming equal variances.

*)

// We test both samples against each other, assuming equal variances.
let twoSampleResult = TTest.twoSample true dataAthletesFemale dataAthletesMale

(*** include-value:twoSampleResult ***)

(**

With a p value of 0.58 the t-test indicate that there's no significant difference between the number of concussions over 3 years between male and female college athletes.


## Two-sample t-test (paired data)

Paired data describes data where each value from the one sample is connected with its respective value from the other sample.  
In the next case, the endurance performance of several persons in a normal situation (control situation) is compared to their performance after ingesting a specific amount of caffeine*. 
It is the same person that performs the exercise but under different conditions. Thus, the resulting values of the persons under each condition are compared.  
Another example are time-dependent experiments: One measures, e.g., the condition of cells stressed with a high surrounding temperature in the beginning and after 30 minutes. 
The measured cells are always the same, yet their conditions might differ.
Due to the connectivity of the sample pairs the samples must be of equal length.

*Source: W.J. Pasman, M.A. van Baak, A.E. Jeukendrup, A. de Haan (1995). _The Effect of Different Dosages of Caffeine on Endurance Performance Time_, International Journal of Sports Medicine, Vol. 16, pp225-230.

*)

let rawDataCaffeine = Http.RequestString @"https://raw.githubusercontent.com/fslaborg/datasets/main/data/CaffeineAndEndurance(wide)_adapted.tsv"

let dataCaffeineAsStream = new System.IO.MemoryStream(rawDataCaffeine |> Encoding.UTF8.GetBytes)
let dataCaffeineAsFrame = Frame.ReadCsv(dataCaffeineAsStream, hasHeaders = false, separators = "\t", schema = "Subject ID, no Dose, 5 mg, 9 mg, 13 mg")

// We want to compare the subjects' performances under the influence of 13 mg caffeine and in the control situation.
let dataCaffeineNoDose, dataCaffeine13mg =
    let getVectorFromCol col = 
        dataCaffeineAsFrame
        |> Frame.getCol col
        |> Series.values
        |> vector
    getVectorFromCol "no Dose", getVectorFromCol "13 mg"

// Transforming our data into a chart.
let visualizePairedData = 
    Seq.zip dataCaffeineNoDose dataCaffeine13mg
    |> Seq.mapi (fun i (control,treatment) -> 
        let participant = "Person " + string i 
        Chart.Line(["no dose", control; "13 mg", treatment], Name = participant)
        )
    |> Chart.combine
    |> Chart.withXAxisStyle ""
    |> Chart.withYAxisStyle("endurance performance", MinMax = (0.,100.))

(**


*)

(*** condition: ipynb ***)
#if IPYNB
visualizePairedData
#endif // IPYNB

(***hide***)
visualizePairedData |> GenericChart.toChartHTML
(***include-it-raw***)

(**



The function for pairwise t-tests can be found at `FSharp.Stats.Testing.TTest.twoSamplePaired`. Note, that the order of the elements in each vector must be the same, so that a pairwise comparison can be performed.

*)

let twoSamplePairedResult = TTest.twoSamplePaired dataCaffeineNoDose dataCaffeine13mg

(*** include-value:twoSamplePairedResult ***)

(**

The two-sample paired t-test suggests a significant difference beween caffeine and non-caffeine treatment groups with a p-value of 0.012. 

*)