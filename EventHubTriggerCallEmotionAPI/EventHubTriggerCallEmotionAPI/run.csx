#r "System.Runtime"
#r "System.Threading.Tasks"
#r "System.IO"

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;


public static void Run(string myEventHubMessage, TraceWriter log)
{
    log.Info($"C# Event Hub trigger function processed a message: {myEventHubMessage}");

    string subscriptionKey = "[Cognitive Services subscriptionKey]";

    var list = ParseJson(myEventHubMessage);

    foreach (var input in list)
    {
        // call Emotion API
        UploadedPhotoStatus result = CallCognitiveAPI(subscriptionKey, input.Url).Result;
        Console.WriteLine(result.Anger);
    }
}


public static List<SensorModel> ParseJson(string s)
{
    var jsondata = File.ReadAllText("./data.json");
    var list = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SensorModel>>(jsondata);

    return list;
}

public static async Task<UploadedPhotoStatus> CallCognitiveAPI(string subscriptionKey, string bloburi)
{
    CloudBlockBlob myBlob = new CloudBlockBlob(new Uri(bloburi));
    var emotionSC = new EmotionServiceClient(subscriptionKey);
    UploadedPhotoStatus uploadedStatus = new UploadedPhotoStatus();
    Emotion[] emotionsResult = await GetEmotionsResult(myBlob, emotionSC);

    try
    {
        double angerTotal = 0;
        double contemptTotal = 0;
        double disgustTotal = 0;
        double fearTotal = 0;
        double happinessTotal = 0;
        double neutralTotal = 0;
        double sadnessTotal = 0;
        double supriseTotal = 0;

        int numOfPerson = emotionsResult.Length;

        if (emotionsResult.Count() > 0)
        {
            foreach (var em in emotionsResult)
            {
                angerTotal += em.Scores.Anger;
                contemptTotal += em.Scores.Contempt;
                disgustTotal += em.Scores.Disgust;
                fearTotal += em.Scores.Fear;
                happinessTotal += em.Scores.Happiness;
                neutralTotal += em.Scores.Neutral;
                sadnessTotal += em.Scores.Sadness;
                supriseTotal += em.Scores.Surprise;
            }

            uploadedStatus.Anger = angerTotal / numOfPerson;
            uploadedStatus.Contempt = contemptTotal / numOfPerson;
            uploadedStatus.Disgust = disgustTotal / numOfPerson;
            uploadedStatus.Fear = fearTotal / numOfPerson;
            uploadedStatus.Happiness = happinessTotal / numOfPerson;
            uploadedStatus.Neutral = neutralTotal / numOfPerson;
            uploadedStatus.Sadness = sadnessTotal / numOfPerson;
            uploadedStatus.Suprise = supriseTotal / numOfPerson;
        }
        else
        {
        }

        return uploadedStatus;

    }
    catch (Exception e)
    {
        return uploadedStatus;
    }
}

public static async Task<Emotion[]> GetEmotionsResult(CloudBlockBlob myBlob, EmotionServiceClient emotionSC)
{
    Emotion[] emotionsResult = null;

    using (var memoryStream = new MemoryStream())
    {
        await myBlob.DownloadToStreamAsync(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);
        emotionsResult = await emotionSC.RecognizeAsync(memoryStream);
    }
    return emotionsResult;
}

public class UploadedPhotoStatus
{
    public int NumOfPerson { get; set; }
    // いとわしさ
    public double Disgust { get; set; }
    // 怒り
    public double Anger { get; set; }
    // 軽蔑
    public double Contempt { get; set; }
    // 恐れ
    public double Fear { get; set; }
    // 幸せ
    public double Happiness { get; set; }
    // 真顔
    public double Neutral { get; set; }
    // 悲しみ
    public double Sadness { get; set; }
    // 驚き
    public double Suprise { get; set; }
}


[JsonObject("sensor")]
public class SensorModel
{
    public string Date { get; set; }
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public double Max { get; set; }
    public double Min { get; set; }
    public string Url { get; set; }
}
