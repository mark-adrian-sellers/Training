
public class TrainingData
{
    public string name { get; set; }
    public Completion[] completions { get; set; }
}

public class Completion
{
    public string name { get; set; }
    public string timestamp { get; set; }
    public string expires { get; set; }
}
