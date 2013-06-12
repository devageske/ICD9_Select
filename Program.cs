using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data.SqlClient;
using System.Data;

namespace TestSummDiagPopulate
{
    
public class Diagnosis
{
    public string SpecimenSubType { get; set; }
    public string SpecDescription { get; set; }
    public string txtDiagnosis { get; set; }
    public decimal ICD9Code { get; set; }
    public string SummaryDiagnosis { get; set; }


    
    public void FillFromCSV(string line)
    {

        string[] parts = line.Split(',');

        SpecimenSubType= parts[3];
        SpecDescription = parts[4];
        txtDiagnosis = parts[5];
        ICD9Code = Convert.ToDecimal(parts[6]);
        SummaryDiagnosis = parts[7];
    }
    public static List<Diagnosis> ReadCSVFile(string filename)
    {

        List<Diagnosis> res = new List<Diagnosis>();

        using (StreamReader sr = new StreamReader(filename))
        {

            string line;
            while ((line = sr.ReadLine()) != null)
            {

                Diagnosis d = new Diagnosis();

                d.FillFromCSV(line);
                res.Add(d);

            }
        }

        return res;
    }
}

public class SummDiag
{

    public decimal ICD9Code { get; set; }
    public string SummaryDiagnosis { get; set; }



    public void FillFromCSV(string line)
    {

        string[] parts = line.Split(',');

        ICD9Code = Convert.ToDecimal(parts[6]);
        SummaryDiagnosis = parts[2];
    }
    public static List<SummDiag> ReadCSVFile(string filename)
    {

        List<SummDiag> res = new List<SummDiag>();

        using (StreamReader sr = new StreamReader(filename))
        {

            string line;
            while ((line = sr.ReadLine()) != null)
            {

                SummDiag sd = new SummDiag();

                sd.FillFromCSV(line);
                res.Add(sd);
            }
        }

        return res;
    }


}
class MatchResult
{
    public decimal result  { get; set; }
    public int caseID { get; set; }
    public int hitCount { get; set; }
    public decimal suggestedICD9 { get; set; }
    public decimal actualICD9 { get; set; }
    public string searchText { get; set; }
    public string actualICD9text { get; set; }
    public string suggestedICD9text { get; set; }

}

class Program
{
    //populate connection string below
    static string connString = "";

    static void Main(string[] args)
    {
        int success = 0;
        int attempt = 31;

        int reportTypeID = 5;
        int reportSubTypeID = 21;

        string outputFolder = ".";
        string csvOut = outputFolder + "\\icd9Output-Derm-" + attempt + ".csv";

        List<Diagnosis> diagList = Diagnosis.ReadCSVFile(outputFolder + "Diagnosis-ICD9-Correlation-Derm-2012.csv");
        List<SummDiag> sdList = SummDiag.ReadCSVFile(outputFolder + "ICD 9 Codes-GI.csv");
        List<MatchResult> resultMatches = new List<MatchResult>();

        DataSet synonyms = getSummaryDiagnosisSynonyms(reportSubTypeID);
        DataSet summaryDiags = getSummaryDiagnosis(reportTypeID, reportSubTypeID);

        // walk through the diagnosis list
        foreach (Diagnosis sampleDiag in diagList)
        {
            MatchResult result = populateSDNew(sampleDiag, summaryDiags, synonyms);

            if (result.result == 0.0M)
            {
                success++;
            }
            resultMatches.Add(result);
        }

        //write out results
        using (System.IO.StreamWriter file = new System.IO.StreamWriter(@csvOut))
        {
            foreach (MatchResult resultOut in resultMatches)
            {
                resultOut.actualICD9text = resultOut.actualICD9text.Replace(',', ' ');
                resultOut.suggestedICD9text = resultOut.suggestedICD9text.Replace(',', ' ');
                string currentLine = resultOut.result + "," + resultOut.hitCount + "," + resultOut.suggestedICD9 + "," + resultOut.suggestedICD9text +
                    "," + resultOut.actualICD9 + "," + resultOut.actualICD9text + "," + resultOut.searchText;

                file.WriteLine(currentLine);

            }
        }

        //.MatchResult.float aveHitCount = resultMatches.AsQueryable.Average<MatchResult>.hitCount;
        float hitRate = (float)success / (float)resultMatches.Count();



    }


    static bool inExcludeList(string sampleTerm)
    {
        switch (sampleTerm.ToLower())
        {
            case "with":
            case "without":
            case "no":
            case "of":
            case "the":
            case "and":
            case "flexure":
            case "body":
            case "skin":
            case "-":
            case "except":
            case "scrotum":
            case "including":
            case "":
            case "in":
            case "parts":
            case "other":
            case "unspecified":
            case "external":
            case "auditory":
            case "canal":
            case "cell":
                return true;
            default:
                return false;
        }

    }

    // Requires a table with SummaryDiagID (autogenerated), ICD9 (text), SummaryDiag (the ICD9 associated text) and reportType and subType (for different specialties)
    static DataSet getSummaryDiagnosis(int reportTypeID, int reportSubTypeID)
    {
        SqlConnection cn = new SqlConnection(connString);
        SqlCommand command = new SqlCommand();
        command.Connection = cn;
        command.CommandType = System.Data.CommandType.Text;
        command.CommandText = "SELECT SummaryDiagID,  ICD9, SummaryDiag  FROM tblSummaryDiag WHERE ReportTypeID=" + reportTypeID + " AND ReportSubTypeID=" + reportSubTypeID + " AND Active=1 ORDER BY SummaryDiag";
        command.CommandTimeout = 0;
        System.Data.DataSet summDiagDS;
        using (SqlDataAdapter da = new SqlDataAdapter())
        {
            da.SelectCommand = command;

            summDiagDS = new System.Data.DataSet();
            da.Fill(summDiagDS);

        }
        return summDiagDS;
    }

    // Input:  ReportSubTypeID refers to Dermatology or Gastro reports. 
    static DataSet getSummaryDiagnosisSynonyms(int reportSubTypeID)
    {
        SqlConnection cn = new SqlConnection(connString);
        SqlCommand command = new SqlCommand();
        command.Connection = cn;
        command.CommandType = System.Data.CommandType.Text;
        command.CommandText = "SELECT * FROM tblSummaryDiagSynonyms WHERE ReportSubTypeID = " + reportSubTypeID;
        command.CommandTimeout = 0;
        System.Data.DataSet summDiagDS;
        using (SqlDataAdapter da = new SqlDataAdapter())
        {
            da.SelectCommand = command;

            summDiagDS = new System.Data.DataSet();
            da.Fill(summDiagDS);

        }
        return summDiagDS;
    }


    // cetteim - this function uses pattern matching to determine a possible best-fit Summary Diagnosis
    // given the Diagnosis text.  Results vary depending on how closely the Diagnosis text matches
    // the available SD (ICD-9s).  A set of synonyms for some of the Summary Diagnosis terms is loaded 
    // from the database to better match the entries.


    static MatchResult populateSDNew(Diagnosis sample, DataSet summaryDiags, DataSet synonyms)
    {
        int frontText = 50;
        int backText = 150;
        double frontBonus = 1.5;
        double backBonus = 0.5;
        int hitCount = -50;
        int bestSD = 0;
        string bestDesc = "";

        MatchResult result = new MatchResult();
        SummDiag bestMatch = new SummDiag();
        // the text to be searched should include the specimen sub-type name
        //string searchText = sample.SpecimenSubType + " " + sample.txtDiagnosis;
        // for derm cases, the description is more important
        string searchText = (sample.SpecDescription + " " + sample.txtDiagnosis).ToLower();

        //check
        result.result = 0.0M;
        result.hitCount = -50;
        result.searchText = searchText;

        int reportSubTypeID = 22;


        if (reportSubTypeID != 0)
        {

            //loop through the summaryDiags
            for (int rowNum = 0; rowNum < summaryDiags.Tables[0].Rows.Count; rowNum++)
            {
                int currentHit = 0;
                string test = (string)summaryDiags.Tables[0].Rows[rowNum]["SummaryDiag"];

                // first test if the whole SD matches
                if (System.Text.RegularExpressions.Regex.IsMatch(searchText, test, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    // ignore the Other SummaryDiag
                    if (test.ToLower() != "other")
                        currentHit += 25;
                }
                string[] terms = test.Split(' ');
                //compare the diagnosis test
                string previousTerm = "";
                foreach (string sampleTerm in terms)
                {
                    if (!inExcludeList(sampleTerm))
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, sampleTerm, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                        {
                            currentHit += 10;

                            //bonus:
                            System.Text.RegularExpressions.Match newMatch = System.Text.RegularExpressions.Regex.Match(sample.txtDiagnosis, sampleTerm, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            if (newMatch.Index < frontText)
                                currentHit += 5;
                            else if (newMatch.Index > backText)
                                currentHit -= 5;

                            if (sampleTerm.ToLower() == "ear" ||
                                sampleTerm.ToLower() == "lip"
                                )
                            {

                                if (!System.Text.RegularExpressions.Regex.IsMatch(searchText, " " + sampleTerm, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                    currentHit -= 10;
                            }

                        }
                        else
                        {
                            // bigger penalty for some terms
                            if (sampleTerm == "Esophagus" || sampleTerm == "Stomach")
                                currentHit -= 15;
                            else
                                currentHit -= 3;  //how much to penalize in general
                        }

                        //now loop through synonyms
                        for (int synRow = 0; synRow < synonyms.Tables[0].Rows.Count; synRow++)
                        {
                            if (sampleTerm.ToLower() == (string)synonyms.Tables[0].Rows[synRow]["SDTerm"])
                            {
                                string possibleSynonym = (string)synonyms.Tables[0].Rows[synRow]["Synonym"];
                                if (System.Text.RegularExpressions.Regex.IsMatch(searchText, possibleSynonym, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                {
                                    double synonymBonus = (int)synonyms.Tables[0].Rows[synRow]["MatchValue"];
                                    System.Text.RegularExpressions.Match newMatch = System.Text.RegularExpressions.Regex.Match(sample.txtDiagnosis, possibleSynonym, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                    if (newMatch.Index < frontText)
                                        synonymBonus *= frontBonus;
                                    else if (newMatch.Index > backText)
                                        synonymBonus *= backBonus;
                                    currentHit += (int)Math.Round(synonymBonus);
                                }
                            }
                        }

                        //special groupings
                        if (sampleTerm.ToLower().Contains("hemor") || sampleTerm.ToLower().Contains("bleeding"))
                        {
                            if (previousTerm == "With")
                            {
                                //if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "gastritis", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                //    currentHit += 15;
                                if (System.Text.RegularExpressions.Regex.IsMatch(searchText, " active", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                    currentHit += 10;
                            }
                            else if ((previousTerm == "No") || (previousTerm == "without"))
                            {
                                if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "inactive", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                    currentHit += 10;
                            }
                        }

                    }
                    previousTerm = sampleTerm;
                }
                if (currentHit >= hitCount)
                {
                    string testICD9 = (string)summaryDiags.Tables[0].Rows[rowNum]["ICD9"];
                    bestSD = (int)summaryDiags.Tables[0].Rows[rowNum]["SummaryDiagID"];
                    bestDesc = (string)summaryDiags.Tables[0].Rows[rowNum]["SummaryDiag"];
                    hitCount = currentHit;

                    result.hitCount = currentHit;
                    bestMatch.SummaryDiagnosis = test;

                    if (testICD9 != "")
                        bestMatch.ICD9Code = (decimal)Convert.ToDecimal(testICD9);
                    else
                        bestMatch.ICD9Code = 0.0M;
                    result.searchText = searchText;
                    result.suggestedICD9text = test;
                    result.result = bestMatch.ICD9Code;

                }

            }


            // if the hitCount is above the threshold, set the SD
            //if (hitCount > 5)
            //{
            //    //set value of SD
            //    ucSummaryDiagnosis.Value = bestSD;
            //    return (bestDesc);
            //}

        }

        result.result = bestMatch.ICD9Code - sample.ICD9Code;
        result.suggestedICD9 = bestMatch.ICD9Code;
        result.actualICD9 = sample.ICD9Code;
        result.actualICD9text = sample.SummaryDiagnosis;


        //score result
        return result;
    }

    // the version without a database of synonyms (and negative correlations).

    static MatchResult populateSD(Diagnosis sample, List<SummDiag> possibleSD)
    {
        MatchResult result = new MatchResult();
        SummDiag bestMatch = new SummDiag();
        // the text to be searched should include the specimen sub-type name
        //string searchText = sample.SpecimenSubType + " " + sample.txtDiagnosis;
        // for derm cases, the description is more important
        string searchText = (sample.SpecDescription + " " + sample.txtDiagnosis).ToLower();

        //check
        result.result = 0.0M;
        result.hitCount = -50;
        result.searchText = searchText;

        foreach (SummDiag test in possibleSD)
        {
            int currentHit = 0;
            // first test if the whole SD matches
            if (System.Text.RegularExpressions.Regex.IsMatch(searchText, test.SummaryDiagnosis, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                currentHit += 25;
            }
            string[] terms = test.SummaryDiagnosis.Split(' ');
            //compare the diagnosis test
            string previousTerm = "";
            foreach (string sampleTerm in terms)
            {
                if (!inExcludeList(sampleTerm))
                {

                    if (System.Text.RegularExpressions.Regex.IsMatch(searchText, sampleTerm, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    {
                        currentHit += 10;
                        if (sampleTerm.ToLower() == "ear" ||
                            sampleTerm.ToLower() == "lip"
                            )
                        {

                            if (!System.Text.RegularExpressions.Regex.IsMatch(searchText, " " + sampleTerm, System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                                || System.Text.RegularExpressions.Regex.IsMatch(searchText, "early", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                currentHit -= 10;
                        }

                        if (sampleTerm.ToLower() == "polyp")
                            currentHit -= 5;

                        //bonus
                        if (sampleTerm.ToLower() == "basal"
                            //    || sampleTerm.ToLower() == "squamous" 
                            || sampleTerm.ToLower() == "melanoma"
                            || sampleTerm.ToLower() == "scalp"
                            )
                            currentHit += 10;
                        if (sampleTerm.ToLower() == "lymphoma")
                            currentHit += 25;
                        if (sampleTerm.ToLower() == "adenoma")
                            currentHit += 20;
                    }
                    else
                    {
                        // bigger penalty for some terms
                        if (sampleTerm == "Esophagus" || sampleTerm == "Stomach")
                            currentHit -= 15;
                        else
                            currentHit -= 5;
                        //derm use -2
                        //currentHit -= 2; //currentHit -= 5;
                    }

                    // now test for synonyms and grouping
                    if (sampleTerm == "Basal")
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "no basal", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit -= 40;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "no evidence of basal", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit -= 40;
                    }


                    if (sampleTerm == "Malignant")
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "carcinoma", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 15;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "atypical", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        //else
                        //   currentHit -= 10;
                    }

                    // Derm specific                        
                    if (sampleTerm.ToLower().Contains("limb"))
                    {
                        if (previousTerm.ToLower() == "upper")
                        {
                            //much more likely to be "with hemorrhage"
                            if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "forearm", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                currentHit += 20;
                            if (System.Text.RegularExpressions.Regex.IsMatch(searchText, " arm", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                currentHit += 20;
                            if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "shoulder", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                currentHit += 20;
                            if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "elbow", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                currentHit += 20;
                            if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "palm", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                currentHit += 20;
                            if (System.Text.RegularExpressions.Regex.IsMatch(searchText, " hand", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                currentHit += 20;
                            if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "finger", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                currentHit += 20;
                            if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "deltoid", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                currentHit += 20;
                            if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "wrist", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                currentHit += 20;
                            if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "clavical", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                currentHit += 20;
                            if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "collar", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                currentHit += 20;
                        }
                        else if ((previousTerm.ToLower() == "lower"))
                        {
                            if (System.Text.RegularExpressions.Regex.IsMatch(searchText, " leg", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                currentHit += 20;
                            if (System.Text.RegularExpressions.Regex.IsMatch(searchText, " knee", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                currentHit += 20;
                            if (System.Text.RegularExpressions.Regex.IsMatch(searchText, " hip", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                currentHit += 20;
                            if (System.Text.RegularExpressions.Regex.IsMatch(searchText, " calf", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                currentHit += 20;
                            if (System.Text.RegularExpressions.Regex.IsMatch(searchText, " thigh", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                currentHit += 20;
                            if (System.Text.RegularExpressions.Regex.IsMatch(searchText, " foot", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                currentHit += 20;
                            if (System.Text.RegularExpressions.Regex.IsMatch(searchText, " ankle", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                currentHit += 20;
                            if (System.Text.RegularExpressions.Regex.IsMatch(searchText, " shin", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                currentHit += 20;
                            if (System.Text.RegularExpressions.Regex.IsMatch(searchText, " toe", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                currentHit += 20;
                            if (System.Text.RegularExpressions.Regex.IsMatch(searchText, " sole", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                currentHit += 20;
                            if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "tibia", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                currentHit += 20;
                            if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "popliteal", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                currentHit += 20;
                            if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "heel", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                currentHit += 20;
                            if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "achilles", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                currentHit += 20;
                        }
                    }
                    if (sampleTerm == "Melanoma")
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "melanoma in situ", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                    }
                    if (sampleTerm == "Eyelid")
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, " lid", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                    }

                    if (sampleTerm == "Ear")
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "helix", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, " auric", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "preauric", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                    }
                    if (sampleTerm == "Trunk")
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "back", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "scapula", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, " chest", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "abdomen", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "buttock", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "axilla", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "groin", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "flank", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "mammary", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "breast", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                    }

                    if (sampleTerm == "Benign")
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, " nevus", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "acanthoma", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "dermatafibroma", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "fibrous papule", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "melanocytic", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 5;
                    }

                    if (sampleTerm == "Situ")
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "in-situ", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "in situ", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                    }
                    if (sampleTerm == "Scalp")
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "crown", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "occipital scalp", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                    }

                    if (sampleTerm == "Face")
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, " temple", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "cheek", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "nose", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "nasal", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "forehead", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "brow", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "jaw", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "zygoma", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, " chin", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, " ala ", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "nasolab", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "nostril", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "sideburn", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "orbital", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "malar", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "occipital", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "mandible", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "nas ", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                    }

                    // GI specific
                    if (sampleTerm == "Duodenitis")
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "duodenum", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 15;
                    }
                    if (sampleTerm.Contains("Barrett"))
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "Barrett", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 15;
                    }
                    if (sampleTerm.ToLower().Contains("stomach"))
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "fundic", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 10;

                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "gastric", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 5;

                    }
                    if (sampleTerm.ToLower().Contains("gastritis"))
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "gastric mucosa", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;

                    }

                    if (sampleTerm.ToLower().Contains("adenoma"))
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "hyperplas", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit += 20;
                    }
                    if (sampleTerm.Contains("Celiac"))
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "no evidence of celiac", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit -= 50;
                    }
                    if (sampleTerm.ToLower() == "dysplasia")
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "no dyplasia", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            currentHit -= 50;
                    }
                    if (sampleTerm.ToLower().Contains("hemor") || sampleTerm.ToLower().Contains("bleeding"))
                    {
                        if (previousTerm == "With")
                        {
                            currentHit += 15;
                            //much more likely to be "with hemorrhage"
                            if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "gastritis", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                currentHit += 15;
                            if (System.Text.RegularExpressions.Regex.IsMatch(searchText, " active", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                currentHit += 10;
                        }
                        else if ((previousTerm == "No") || (previousTerm == "without"))
                        {
                            if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "inactive", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                currentHit += 10;
                        }
                    }
                }

                if (sampleTerm.ToLower() == ("colitis"))
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "colonic mucosa", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                        currentHit += 20;
                    if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "colon ", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                        currentHit += 20;
                    if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "small bowel", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                        currentHit += 20;
                    if (System.Text.RegularExpressions.Regex.IsMatch(searchText, "ileitis", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                        currentHit += 20;

                }

                previousTerm = sampleTerm;
            }

            // establish a hit score
            if (currentHit > result.hitCount)
            {
                result.hitCount = currentHit;
                bestMatch = test;
                result.searchText = searchText;
                result.suggestedICD9text = test.SummaryDiagnosis;
            }
        }

        result.result = bestMatch.ICD9Code - sample.ICD9Code;
        result.suggestedICD9 = bestMatch.ICD9Code;
        result.actualICD9 = sample.ICD9Code;
        result.actualICD9text = sample.SummaryDiagnosis;


        //score result
        return result;
    }



    // cetteim - this function uses pattern matching to determine a possible best-fit Summary Diagnosis
    // given the Diagnosis text.  Results vary depending on how closely the Diagnosis text matches
    // the available SD (ICD-9s).  A set of synonyms for some of the Summary Diagnosis terms is loaded 
    // from the database to better match the entries.


}
}
