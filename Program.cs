// Program created by Mark A. Sellers

var appSettings = JsonFileHandler.Read<AppSettings>("appsettings.json");

var trainingData = JsonFileHandler.Read <List<TrainingData>>(appSettings.InputFilePath);

createOutputFile1(
    trainingData, 
    appSettings.OutputFilePath1
    );

var latestTraining = LatestTraining(trainingData);
createOutputFile2(
    trainingData, 
    latestTraining, 
    appSettings.TrainingTypes, 
    appSettings.FiscalYear, 
    appSettings.OutputFilePath2
    );

var statusDate = DateTime.Parse(appSettings.StatusDate);
createOutputFile3(
    trainingData, 
    statusDate, 
    latestTraining, 
    appSettings.OutputFilePath3
    );

/*
    A note for all tasks. It is possible for a person to have completed the same training more than once. 
    In this event, only the most recent completion should be considered.
*/
    
// create a lookup table for latest training date for each trainee, for each training type

Dictionary<string, Dictionary<string, DateTime>> LatestTraining(List<TrainingData> trainingData)
{ 
    var latestTraining = new Dictionary<string, Dictionary<string, DateTime>>();

    List<string> traineeNames = trainingData
    .Select(t => t.name)
    .Distinct()
    .ToList();

    traineeNames.ForEach(n => latestTraining[n] = new Dictionary<string, DateTime>());

    foreach (var trainee in trainingData)
    {
        foreach (var c in trainee.completions)
        {
            var d1 = DateTime.MinValue;
            latestTraining[trainee.name].TryGetValue(c.name, out d1);

            var d2 = DateTime.Parse(c.timestamp);

            latestTraining[trainee.name][c.name] = DateTime.Compare(d1, d2) > 0 ? d1 : d2;
        }
    }

    return latestTraining;
}

// List each completed training with a count of how many people have completed that training.
void createOutputFile1(
    List<TrainingData> trainingData, 
    string outputPath
    )
{
    var trainingTypeCounts = new Dictionary<string, int>();

    // create a list of all training types
    var trainingTypes = trainingData
        .SelectMany(t => t.completions.Select(c => c.name))
        .Distinct()
        .Order()
        .ToList();
    
    trainingTypes.ForEach(t =>
    {
        // count all trainees that have any occurrences of each training type
        // note: if any trainees have a split or duplicate record in the original data, this could count them twice
        //
        var count = trainingData
            .Where(td => td.completions.Any(c => c.name == t))
            .Count();
        trainingTypeCounts[t] = count;
    }
    );

    // write the output file
    JsonFileHandler.Write(
        trainingTypeCounts, 
        outputPath
        );
}

/*
 Given a list of trainings and a fiscal year (defined as 7/1/n-1 – 6/30/n), 
for each specified training, list all people that completed that training in the specified fiscal year.
Use parameters: Trainings = "Electrical Safety for Labs", "X-Ray Safety", "Laboratory Safety Training"; Fiscal Year = 2024
 */
void createOutputFile2( 
    List<TrainingData> trainingData, 
    Dictionary<string, Dictionary<string, DateTime>> latestTraining, 
    string[] trainingTypes,
    int trainingFiscalYear,
    string outputPath 
    )
{
    var trainingCompletionList = new Dictionary<string, List<string>>();

    // for each of the particular training types we are checking
    foreach (var t in trainingTypes)
    {
        // use a HashSet so no one is included twice for the same training type
        // this would only be a concern if they took the same training more than once in the same fiscal year
        var tmpList = new HashSet<string>();


        foreach (var trainee in trainingData)
        {
            // for any training completions that match the particular type we are checking this iteration
            foreach (var c in trainee.completions.Where(n => n.name == t))
            {
                // we only consider it if it is the latest training for this person, for this training type
                if (DateTime.Parse(c.timestamp) == latestTraining[trainee.name][c.name])
                {
                    // if the training matches the fiscal year, add it to the list
                    var fiscalYear = DateTime.Parse(c.timestamp).UofIFiscalYear();
                    if (fiscalYear == trainingFiscalYear)
                    {
                        tmpList.Add(trainee.name);
                    }
                }
            }
        }

        // sort list by surname
        trainingCompletionList[t] = tmpList
            .OrderBy(n => n.Split(" ").Last())
            .ToList();

    }

    JsonFileHandler.Write(
        trainingCompletionList, 
        outputPath
        );
}

/*
    Given a date, find all people that have any completed trainings that have already expired, 
    or will expire within one month of the specified date 
    (A training is considered expired the day after its expiration date). 
    For each person found, list each completed training that met the previous criteria, 
    with an additional field to indicate expired vs expires soon.
    Use date: Oct 1st, 2023
*/
void createOutputFile3(
    List<TrainingData> trainingData, 
    DateTime statusDate, 
    Dictionary<string, Dictionary<string, DateTime>> latestTraining, 
    string outputPath)
{
    var trainingStatus = new Dictionary<string, Dictionary<string, string>>();

    // order our trainees alphabetically by surname before processing

    foreach (var trainee in trainingData.OrderBy(t => t.name.Split(" ").Last()))
    {
        // ensure this completion record has an expiration
        // and is the latest training completed for trainee & training type    
        foreach (var c in trainee.completions
            .Where(ts =>
                ts.expires != null
                &&
                (
                    DateTime.Parse(ts.timestamp)
                    ==
                    latestTraining[trainee.name][ts.name]
                )
            ))
        {

            var expireDate = DateTime.Parse(c.expires);
            var status = "";

            if (DateTime.Compare(expireDate, statusDate) == -1)
            {
                status = "Expired";
            }
            else
            {
                var soonDate = expireDate.AddMonths(-1);
                if (DateTime.Compare(soonDate, statusDate) == -1)
                {
                    status = "Expiring Soon";
                }
            }

            if (status != "")
            {
                if (!trainingStatus.ContainsKey(trainee.name))
                {
                    trainingStatus[trainee.name] = new Dictionary<string, string>();
                }
                trainingStatus[trainee.name][c.name] = status;

            }
        }

        JsonFileHandler.Write(
            trainingStatus, 
            outputPath
            );
    }
}
