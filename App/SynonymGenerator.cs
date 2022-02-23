using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;


using System.Xml.Serialization;
using System.IO;
using System.Configuration;
using System.Net;
using System.Xml.XPath;

using System.Data;
using System.Data.SqlClient;

using java.io;
using edu.stanford.nlp.ling;
using edu.stanford.nlp.tagger.maxent;
using edu.stanford.nlp.parser;
using java.util;
using edu.stanford.nlp.ie.crf;
using edu.stanford.nlp.pipeline;
using edu.stanford.nlp.util;
using System.Text.RegularExpressions;
using Console = System.Console;
using System.Threading;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace SynonymWebService
{
    public class Verb
    {
        public string Verbn;
        public string Present;
        public string Past;
        public string Past2nd;
        public string Plural;
        public string Plural2nd;
    }


    public class AliasLookup
    {
        public string Keyword { get; set; }

        public List<string> Alias { get; set; }
    }

    public class StringSplitters
    {
        public string[] SplitCamelCase(string source)
        {
            var res = Regex.Split(source, @"(?<!^)(?=[A-Z])");

            var resTotal = new List<string>();

            for (int i = 0; i < res.Count(); i++)
            {
                if ((i - 1 > 0))
                {
                    if (res[i].Length == 1 && res[i - 1].Length == 1)
                    {
                        var finalWord = res[i - 1] + res[i];
                        resTotal.Add(finalWord);
                    }
                    if (res[i].Length > 1)
                    {
                        resTotal.Add(res[i]);
                    }
                }
                else
                {
                    if (res[i].Length > 1)
                    {
                        resTotal.Add(res[i]);
                    }
                }
            }


            return resTotal.ToArray();
        }
    }

    public static class SynonymGenerator
    {
        private static string[] _illegals;

        private static CRFClassifier _classifierNER;
        private static MaxentTagger _tagger;
        public static StanfordCoreNLP _pipeline;

        private static List<string> _IN_Words = new List<string>() { "in", "during", "from", "since", "for", "of", "" };
        private static List<string> _DT_Words = new List<string>() { "an", "a", "the", "" };

        private static List<Verb> _verbList = new List<Verb>();

        private static List<AliasLookup> _aliasLookups;

        private static DataModel _keywordDataModel;

        private static readonly log4net.ILog _log;

        private static List<string> _dictKeywordUserFriendly;

        public static void WriteToFileLog(string log)
        {
            var path = System.Configuration.ConfigurationManager.AppSettings["OUTPUT_PATH"].ToString();
            if (!System.IO.File.Exists(path))
            {
                //crude approach
                using (System.IO.FileStream fs = System.IO.File.Create(path)) { }
            }

            using (StreamWriter writer = new StreamWriter(path))
            {
                writer.WriteLine(log + " :: " + DateTime.Now.ToString());
            }
        }

        static SynonymGenerator()
        {
            _log = log4net.LogManager.GetLogger
                (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            try
            {

                var temp = ConfigurationManager.AppSettings["STANDFORD_NLP_NER"].ToString();


                _classifierNER = CRFClassifier.getClassifierNoExceptions(System.Configuration.
        ConfigurationManager.AppSettings["STANDFORD_NLP_NER"].ToString());

                _tagger = new MaxentTagger(System.Configuration.
                    ConfigurationManager.AppSettings["STANDFORD_NLP_POSTTAGGER"].ToString());


                _illegals = new string[]
                {
                "www.", ".com", ".net", ".tv", "...", "..", "....", "#", "&", ",", "en.", ".org", "!", "_",".gov", ".wales",".voyage",".wtf",".ngo",
                ".nl",".dk",".buzz",".com.au",".id.au",".at",".art", ".co.uk",".uk",".co",".org.uk",".me.uk",".me",".london",".scot",".io",".eu",
                ".club",
                ".design",
                ".ac",
                ".academy",
                ".accountant",
                ".accountants",
                ".actor",
                ".adult",
                ".ae.org",
                ".ae",
                ".af",
                ".africa",
                ".ag",
                ".agency",
                ".am",
                ".apartments",
                ".com.ar",
                ".archi",
                ".art",
                ".as",
                ".asia",
                ".associates",
                ".at",
                ".attorney",
                ".com.au",
                ".id.au",
                ".net.au",
                ".org.au",
                ".auction",
                ".band",
                ".bar",
                ".bargains",
                ".bayern",
                ".be",
                ".beer",
                ".berlin",
                ".best",
                ".bet",
                ".bid",
                ".bike",
                ".bingo",
                ".bio",
                ".biz",
                ".black",
                ".blog",
                ".blue",
                ".boutique",
                ".br.com",
                ".brussels",
                ".build",
                ".builders",
                ".business",
                ".buzz",
                ".bz",
                ".ca",
                ".cab",
                ".cafe",
                ".cam",
                ".camera",
                ".camp",
                ".capetown",
                ".capital",
                ".cards",
                ".care",
                ".career",
                ".careers",
                ".casa",
                ".cash",
                ".casino",
                ".catering",
                ".cc",
                ".center",
                ".ch",
                ".chat",
                ".cheap",
                ".church",
                ".city",
                ".cl",
                ".claims",
                ".cleaning",
                ".click",
                ".clinic",
                ".clothing",
                ".cloud",
                ".cm",
                ".cn.com",
                ".uk.net",
                ".coach",
                ".codes",
                ".coffee",
                ".college",
                ".cologne",
                ".community",
                ".company",
                ".computer",
                ".condos",
                ".construction",
                ".consulting",
                ".contractors",
                ".xyz",
                ".uk.com",
                ".zone"
                };

                LoadCoreNLP();

                PopulateVerb();

                LoadLocalLookupWords();

                _keywordDataModel = new DataModel();


                LoadUFKeywordHelper();

            }
            catch (Exception e)
            {
                WriteToFileLog("SynonymGenerator() :: " + e.Message);
            }
        }

        private static void LoadUFKeywordHelper()
        {
            _dictKeywordUserFriendly = _keywordDataModel.GetWordContains();
        }

        private static void LoadLocalLookupWords()
        {
            _aliasLookups = new List<AliasLookup>();

            var alias = new AliasLookup()
            {
                Alias = new List<string>() { "identification", "i.d.", "i.d" },
                Keyword = "id"
            };
            _aliasLookups.Add(alias);

            var aliasn = new AliasLookup()
            {
                Alias = new List<string>() { "id", "i.d.", "i.d" },
                Keyword = "identification"
            };
            _aliasLookups.Add(aliasn);


            var aliasw = new AliasLookup()
            {
                Alias = new List<string>() { "identification", "id", "i.d" },
                Keyword = "i.d."
            };
            _aliasLookups.Add(aliasw);


            var aliasu = new AliasLookup()
            {
                Alias = new List<string>() { "id", "i.d.", "identification" },
                Keyword = "i.d"
            };
            _aliasLookups.Add(aliasu);


            var aliasp = new AliasLookup()
            {
                Alias = new List<string>() { "amount" },
                Keyword = "amt"
            };
            _aliasLookups.Add(aliasp);

        }

        private static void LoadCoreNLP()
        {
            var jarRoot = System.Configuration.
                ConfigurationManager.AppSettings["STANDFORD_NLP_CORE"].ToString();

            // Annotation pipeline configuration
            var props = new Properties();
            props.setProperty("annotators", "tokenize, ssplit, pos, lemma, ner, parse, dcoref");
            props.setProperty("ner.useSUTime", "0");

            // We should change current directory, so StanfordCoreNLP could find all the model files automatically
            var curDir = Environment.CurrentDirectory;
            System.IO.Directory.SetCurrentDirectory(jarRoot);

            _pipeline = new StanfordCoreNLP(props);
            System.IO.Directory.SetCurrentDirectory(curDir);
        }

        public static Synonym GetSynonyms(string searchWords, bool checkNLPdatabase)
        {
            //1.check google
            //2.check bing
            //3.thesauras 

            return GenerateSynonyms(searchWords, checkNLPdatabase);
        }

        /// <summary>
        /// /Used to get straight forward synonyms, this version is not used as part of a bigger method
        /// </summary>
        /// <param name="searchWords"></param>
        /// <param name="checkNLPdatabase"></param>
        /// <returns></returns>
        public static Synonym GenerateSynonymsSingleWordFull(string searchWords, bool checkNLPdatabase)
        {
            var synonym = new Synonym();

            var wordCount = searchWords.Split();
            var wordListing = new List<string>();

            if (wordCount.Count() == 1)
            {
                //implement wordAPI                
                wordListing.AddRange(GetDataFromWordAPI(searchWords));

                //single word results
                wordListing.AddRange(ThesaurusWords(searchWords));
                if (wordListing.Any() == false)
                {
                    wordListing = GetSynonymFromNet(searchWords);
                }
            }
            synonym.SuggestedPrimaryWords = searchWords;
            synonym.SearchWords = searchWords;
            synonym.SynonymWords = wordListing;

            return synonym;
        }

        private static string GetUserfriendlyName(string searchWords, out string original)
        {
            original = "";

            var wordCollection = new List<string>();

            foreach (var item in _dictKeywordUserFriendly)
            {
                if (searchWords.Contains(item))
                {
                    wordCollection.Add(item);
                }
            }

            if (wordCollection.Count > 1)
            {
                var added = new List<string>();

                var lengthiest = wordCollection.OrderByDescending(m => m.Length).FirstOrDefault();
                string[] separatingStrings = { lengthiest };
                var final = searchWords.Split(separatingStrings, StringSplitOptions.RemoveEmptyEntries);


                var secondLengthiest = wordCollection.Where(mj => mj != lengthiest).OrderByDescending(m => m.Length).FirstOrDefault();
                string[] separatingStringsRebot = { secondLengthiest };
                var ans = final.LastOrDefault().Split(separatingStringsRebot, StringSplitOptions.RemoveEmptyEntries);

                if (ans.FirstOrDefault() != final.LastOrDefault())
                {
                    //rules for adding more

                    if (lengthiest.Substring(0, 3) == searchWords.Substring(0, 3)
                        && ans.FirstOrDefault().Substring(ans.FirstOrDefault().Count() - 3, 3) == searchWords.Substring(searchWords.Count() - 3, 3))
                    {
                        added.Add(lengthiest);
                        added.Add(secondLengthiest);
                        added.Add(ans.FirstOrDefault());
                    }
                    else if (lengthiest.Substring(lengthiest.Count() - 3, 3) == searchWords.Substring(searchWords.Count() - 3, 3)
                        && ans.FirstOrDefault().Substring(0, 3) == searchWords.Substring(0, 3))
                    {
                        added.Add(ans.FirstOrDefault());                    
                        added.Add(secondLengthiest);
                        added.Add(lengthiest);
                    }
                    else if (secondLengthiest.Substring(secondLengthiest.Count() - 3, 3) == searchWords.Substring(searchWords.Count() - 3, 3)
                        && ans.FirstOrDefault().Substring(0, 3) == searchWords.Substring(0, 3))
                    {
                        added.Add(ans.FirstOrDefault());
                        added.Add(lengthiest);
                        added.Add(secondLengthiest);
                    }
                    else if (lengthiest.Substring(0, 3) == searchWords.Substring(0, 3)
                        && secondLengthiest.Substring(secondLengthiest.Count() - 3, 3) == searchWords.Substring(searchWords.Count() - 3, 3))
                    {
                        added.Add(lengthiest);                        
                        added.Add(ans.FirstOrDefault());
                        added.Add(secondLengthiest);                        
                    }
                    else
                    {
                        added.Add(secondLengthiest);
                        added.Add(ans.FirstOrDefault());
                        added.Add(lengthiest);                       
                    }

                    original = searchWords;

                    var seperatedString = string.Join(" ", added);
                    return seperatedString;

                }
                else
                {
                    if (lengthiest.Substring(0, 3) == searchWords.Substring(0, 3))
                    {
                        added.Add(lengthiest);
                        added.Add(final.LastOrDefault());
                    }
                    else
                    {
                        added.Add(final.LastOrDefault());
                        added.Add(lengthiest);
                    }

                    original = searchWords;
                    var seperatedString = string.Join(" ", added);
                    return seperatedString;
                }

            }

            return searchWords;
        }

        private static bool IsLowerCase(string searchWords)
        {
            return searchWords.All(char.IsLower);
        }

        private static string TextReplace(string searchWords)
        {
            return searchWords.Replace("_", " ");
        }

        public static Synonym GenerateSynonyms(string searchWords, bool checkNLPdatabase)
        {
            var synonym = new Synonym();
            try
            {
                var original = "";
                var replace = "";

                searchWords = TextReplace(searchWords);

                if (IsLowerCase(searchWords))
                {
                    searchWords = GetUserfriendlyName(searchWords, out original);
                }

                var wordCount = searchWords.Split();
                var wordListing = new List<string>();

                //if (wordCount.Count() == 1)
                //{
                var stringSplitters = new StringSplitters();

                wordCount = stringSplitters.SplitCamelCase(searchWords);

                if (wordCount.Count() > 1)
                {
                    replace = string.Join(" ", wordCount);
                }
                // }

                if (wordCount.Count() == 1)
                {
                    //implement wordAPI                
                    var res = GetDataFromWordAPI(searchWords);
                    if (res != null)
                    {
                        if (res.Count > 4)
                        {
                            wordListing = res.GetRange(0, 4);
                        }
                    }

                    if (wordListing.Any() == false)
                    {
                        res = ThesaurusWords(searchWords);

                        if (res != null)
                        {
                            wordListing.AddRange(res);

                            // wordListing.AddRange(res.Count > 4 ? res.GetRange(0, 4) : res);

                            //if (wordListing.Count > 4)
                            //{
                            //    wordListing = wordListing.GetRange(0, 4);
                            //}
                        }
                    }
                }
                else
                {
                    //mulitple words results
                    //take word splits and create combos from thesauras results
                    //then qualify the combos via google/net
                    //var wordListing = ThesaurusWords(searchWords);

                    // var initialWordListing = GetSynonymFromNet(searchWords);

                    var initialWordListing = AutoGenerate(searchWords);
                    //if (initialWordListing != null)
                    //{
                    //    if (initialWordListing.Count > 4)
                    //    {
                    //        initialWordListing = initialWordListing.GetRange(0, 4);
                    //    }
                    //}

                    wordListing = initialWordListing;
                }

                if (String.IsNullOrEmpty(original) == false)
                {
                    wordListing.Add(original.ToLower());
                }
                else if (String.IsNullOrEmpty(replace) == false)
                {
                    wordListing.Add(searchWords.ToLower());
                    searchWords = replace.ToLower();
                }

                synonym.SuggestedPrimaryWords = searchWords.ToLower();
                synonym.SearchWords = searchWords.ToLower();
                synonym.SynonymWords = wordListing;
            }
            catch (Exception ex)
            {
                WriteToFileLog("GenerateSynonyms() :: " + ex.Message);

                _log.Error(ex.Message);
            }
            return synonym;
        }

        private static void GenWordCombos(List<string> final, List<KeyValuePair<string, List<string>>> collection, string previousWord, int iter)
        {
            iter++;

            if (iter < collection.Count)
            {
                foreach (var ven in collection[iter].Value)
                {
                    var newCreationdd = previousWord + ven + " ";

                    if (string.IsNullOrEmpty(ven))
                    {
                        newCreationdd = previousWord;
                    }
                    GenWordCombos(final, collection, newCreationdd, iter);
                }
            }
            else
            {
                final.Add(previousWord.Trim());
            }
        }

        private static List<string> AutoGenerate(string searchWords)
        {
            var final = new List<string>();

            var wordCount = searchWords.Split();

            //if (wordCount.Count() == 1)
            //{
            var stringSplitters = new StringSplitters();

            wordCount = stringSplitters.SplitCamelCase(searchWords);
            //}

            var collection = new List<KeyValuePair<string, List<string>>>();

            if (wordCount.Count() == 3)
            {
                var digit = wordCount.Where(mmm => mmm.Any(hn => char.IsDigit(hn)));
                if (digit.Any())
                {
                    var nonCount = wordCount.Where(mmm => mmm != digit.FirstOrDefault());
                    foreach (var ib in nonCount)
                    {
                        var list = ThesaurusWords(ib);

                        var str = digit.FirstOrDefault() + " " + ib;
                        final.Add(str);
                        str = ib + " " + digit.FirstOrDefault();
                        final.Add(str);

                        foreach (var item in list)
                        {
                            str = digit.FirstOrDefault() + " " + item;
                            final.Add(str);

                            str = item + " " + digit.FirstOrDefault();
                            final.Add(str);
                        }
                    }
                }
            }

            if (wordCount.Count() < 5)
            {
                foreach (var item in wordCount)
                {
                    var synValues = GetWordDataAlias(item);
                    collection.Add(new KeyValuePair<string, List<string>>(item, synValues));
                }

                int iter = 0;
                foreach (var ven in collection[0].Value)
                {
                    var newCreationvv = "" + ven + " ";
                    GenWordCombos(final, collection, newCreationvv, iter);
                }

                if (final.Any())
                {
                    final = final.Where(mm => mm != searchWords).ToList();
                }
            }
            else
            {
                final.Add(searchWords.ToLower());
            }

            return final;
        }

        private static List<string> GetWordDataAlias(string searchWords)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;


            // var data = GetTextTagsMetadataEverything(searchWords);

            var res = new List<string>();
            var resDict = new List<string>();

            if (_IN_Words.Any(m => m == searchWords))
            {
                return _IN_Words;
                //return _IN_Words.Where(g => g != searchWords).ToList();
            }
            else if (_DT_Words.Any(m => m == searchWords))
            {
                return _DT_Words;
                //return _DT_Words.Where(g => g != searchWords).ToList();
            }
            else if ("to" == searchWords)
            {
                res.Add("to");
            }
            else
            {
                resDict = LocalLookUpWords(searchWords);
                //resDict = DictionaryWords(searchWords);
                res.AddRange(resDict);
            }

            if (resDict.Any() == false)
            {
                var thesRes = ThesaurusWords(searchWords);
                foreach (var item in thesRes)
                {
                    if (res.Any(mm => mm == item) == false && string.IsNullOrEmpty(item) == false)
                    {
                        res.Add(item);
                    }
                }
                //res.AddRange(ThesaurusWords(searchWords));
            }

            var resPastTense = DictionaryWordsPastTenseEtc(searchWords);
            foreach (var item in resPastTense)
            {
                if (res.Any(mm => mm == item) == false && string.IsNullOrEmpty(item) == false)
                {
                    res.Add(item);
                }
            }
            res.Insert(0, searchWords);


            for (int i = 0; i < res.Count(); i++)
            {
                res[i] = res[i].ToLower();
            }

            return res;
        }

        private static List<string> QualifyByContent(string searchWords, List<string> wordListing)
        {
            var qualfiedList = new List<string>();

            var searchWordSplits = searchWords.Split();
            var count = searchWordSplits.Count();

            bool hasNumber = false;
            if (searchWords.Any(c => char.IsDigit(c))) hasNumber = true;


            foreach (var item in wordListing)
            {
                var currentSplit = item.Split();
                double match = 0;

                bool candidateHasNumber = false;
                if (item.Any(c => char.IsDigit(c))) candidateHasNumber = true;

                foreach (var itemFound in currentSplit)
                {
                    if (searchWordSplits.Any(m => m.Contains(itemFound)))
                    {
                        match++;
                    }
                }

                var percentage = (match / count) * 100.0;
                var threshold = count <= 2 ? 90.0 : 60.0;

                if ((hasNumber == false && percentage >= threshold) || (hasNumber && candidateHasNumber && percentage > threshold))
                {
                    qualfiedList.Add(item);
                }
            }
            return qualfiedList;

            //Add NER
            //return QualifyByPartSpeechTag(searchWords, qualfiedList);
        }

        private static List<string> QualifyByPartSpeechTag(string searchWords, List<string> wordListing)
        {
            //var searchWordTags = GetTextTagsMetadata(searchWords);
            var collectionCorrect = new List<string>();

            var searchWordTags = NamedEntityRecognition(searchWords);

            foreach (var item in wordListing)
            {
                //var wordTagsListing = GetTextTagsMetadata(item);

                var wordTagsListing = NamedEntityRecognition(item);
                bool exists = true;

                foreach (var im in searchWordTags)
                {
                    if (wordTagsListing.Any(vb => vb.Value == im.Value) == false)
                    {
                        exists = false;
                        break;
                    }
                }

                if (exists && wordTagsListing.Any() && searchWordTags.Any())
                {
                    collectionCorrect.Add(item);
                }
                else if (exists && wordTagsListing.Count == 0 && searchWordTags.Count == 0)
                {
                    collectionCorrect.Add(item);
                }
            }
            return collectionCorrect;
        }

        private static void ExtractedTag(HtmlDocument document, string tag, List<KeyValuePair<string, string>> collections)
        {
            var tagsData = document.DocumentNode.SelectNodes("//" + tag);
            if (tagsData != null)
            {
                foreach (var pr in tagsData)
                {
                    //prevent duplication of tags
                    if (collections.Any(m => m.Value == pr.InnerHtml.ToLower() && m.Key == tag) == false)
                    {
                        collections.Add(new KeyValuePair<string, string>(tag, pr.InnerText.ToLower()));
                    }
                }
            }
        }

        public static List<KeyValuePair<string, string>> NamedEntityRecognition(string text)
        {
            string tag = "person";
            string tagSecond = "organization";
            string tagThird = "location";
            var collections = new List<KeyValuePair<string, string>>();

            var document = new HtmlDocument();
            document.LoadHtml(_classifierNER.classifyWithInlineXML(text.ToUpper()));

            ExtractedTag(document, tag, collections);
            ExtractedTag(document, tagSecond, collections);
            ExtractedTag(document, tagThird, collections);

            return collections;
        }

        public static List<string> GetTextTagsMetadataEverything(String documentText)
        {
            var list = new List<string>();

            var lemStr = documentText;

            // create an empty Annotation just with the given text
            var document = new Annotation(documentText);

            // run all Annotators on this text
            _pipeline.annotate(document);

            // Iterate over all of the sentences found
            var sentences = document.get(new CoreAnnotations.SentencesAnnotation().getClass()) as ArrayList;

            foreach (CoreMap sentence in sentences)
            {
                var otherToken = document.get(new CoreAnnotations.TokensAnnotation().getClass()) as ArrayList;

                // Iterate over all tokens in a sentence
                foreach (CoreLabel token in otherToken)
                {
                    var originalWord = (string)token.get(new CoreAnnotations.OriginalTextAnnotation().getClass()) as string;
                    var lem = (string)token.get(new CoreAnnotations.LemmaAnnotation().getClass()) as string;

                    var pos = (string)token.get(new CoreAnnotations.PartOfSpeechAnnotation().getClass()) as string;

                    list.Add(pos);
                }
            }
            return list;
        }

        public static List<string> GetTextTagsMetadata(String documentText)
        {
            var list = new List<string>();

            var lemStr = documentText;

            // create an empty Annotation just with the given text
            var document = new Annotation(documentText);

            // run all Annotators on this text
            _pipeline.annotate(document);

            // Iterate over all of the sentences found
            var sentences = document.get(new CoreAnnotations.SentencesAnnotation().getClass()) as ArrayList;

            foreach (CoreMap sentence in sentences)
            {
                var otherToken = document.get(new CoreAnnotations.TokensAnnotation().getClass()) as ArrayList;

                // Iterate over all tokens in a sentence
                foreach (CoreLabel token in otherToken)
                {
                    var originalWord = (string)token.get(new CoreAnnotations.OriginalTextAnnotation().getClass()) as string;
                    var lem = (string)token.get(new CoreAnnotations.LemmaAnnotation().getClass()) as string;

                    var pos = (string)token.get(new CoreAnnotations.PartOfSpeechAnnotation().getClass()) as string;

                    list.Add(pos);
                }
            }
            return list;
        }


        private static List<string> GetSynonymFromNet(string searchWords)
        {
            var wordListing = new List<string>();

            var searchEngineWordListing = SearchEngineThesaurus(searchWords, System.Configuration.ConfigurationManager.AppSettings["SEARCH_ENGINE_ONE"].ToString());

            var doubleCheckedWordListing = SearchEngineThesaurus(searchWords, System.Configuration.ConfigurationManager.AppSettings["SEARCH_ENGINE_TWO"].ToString());

            wordListing.AddRange(searchEngineWordListing);
            wordListing.AddRange(doubleCheckedWordListing);

            var qualified = searchEngineWordListing.Where(m => doubleCheckedWordListing.Any(h => h == m)).ToList();

            //if (qualified.Any()) return qualified;

            if (wordListing.Any()) wordListing = wordListing.Distinct().ToList();

            return wordListing;
        }

        private static void PopulateVerb()
        {
            using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["NLP-DATA"].ToString()))
            {
                var qu = "proc_GetVerblist";
                using (SqlCommand cmd = new SqlCommand(qu, cn))
                {
                    cn.Open();

                    SqlDataReader dataReader = cmd.ExecuteReader();
                    if (dataReader.HasRows)
                    {
                        while (dataReader.Read())
                        {
                            var verb = new Verb();
                            verb.Verbn = dataReader["Verb"].ToString();
                            verb.Present = dataReader["Present"].ToString();
                            verb.Past = dataReader["Past"].ToString();
                            verb.Past2nd = dataReader["Past2nd"].ToString();
                            verb.Plural = dataReader["Plural"].ToString();
                            verb.Plural2nd = dataReader["Plural2nd"].ToString();

                            _verbList.Add(verb);
                        }
                    }
                }
            }
        }

        public static bool GenerateSynonymsAutomatic(string dagId)
        {
            var collection = new List<Synonym>();
            string path = "";
            WriteToFileLog("genertaing the data " + dagId);

            try
            {
                if (dagId == "default")
                {
                    dagId = "universal";

                    var masterList = _keywordDataModel.GetDefaultTypesData();

                    collection.AddRange(masterList);
                }
                else
                {
                    var masterList = _keywordDataModel.GetDatasetMetaData(dagId);

                    foreach (var item in masterList)
                    {
                        var res = GenerateSynonyms(item.SuggestedPrimaryWords, true);
                        if (res.SynonymWords != null)
                        {
                            res.TypeData = item.TypeData;
                            res.OriginalName = item.OriginalName;
                            if (res.TypeData == "column")
                            {
                                res.Parent = item.Parent;
                            }
                            collection.Add(res);
                        }
                    }
                }


                path = WriteToCSVFileForDB(collection, dagId);
            }
            catch (Exception ex)
            {
                WriteToFileLog(ex.Message);
            }

            //check nlp
            //insert into nlp
            return WriteToDB(collection, path);
        }

        private static string WriteToCSVFileForDB(List<Synonym> synonymGroups, string dagID)
        {
            var directoryOutput = ConfigurationManager.AppSettings["NLP_IMPPORT"];

            if (Directory.Exists(directoryOutput) == false)
            {
                Directory.CreateDirectory(directoryOutput);
            }


            String fileName = "Temp_" + dagID +".csv";

            String fullPath = directoryOutput + "\\" + fileName;
            if (!System.IO.File.Exists(fullPath))
            {
                //crude approach
                using (System.IO.FileStream fs = System.IO.File.Create(fullPath)) { }
            }
            else
            {
                System.IO.File.Delete(fullPath);
                using (System.IO.FileStream fs = System.IO.File.Create(fullPath)) { }
            }

            try
            {
                using (CsvFileWriter writer = new CsvFileWriter(fullPath))
                {
                    foreach (var syn in synonymGroups) //result.Items
                    {
                        if (syn.SynonymWords != null)
                        {
                            foreach (var item in syn.SynonymWords)
                            {
                                var guid = Guid.NewGuid().ToString();
                                CsvRow csvRow = new CsvRow();

                                csvRow.Add(guid);

                                csvRow.Add(dagID);

                                csvRow.Add(syn.SuggestedPrimaryWords.ToLower());

                                //TRTokenType
                                if (syn.TypeData == "table")
                                {
                                    csvRow.Add("operation");
                                }
                                else if (syn.TypeData == "column")
                                {
                                    csvRow.Add("subOperation");
                                }
                                else
                                {
                                    csvRow.Add(syn.Parent);
                                }

                                //------------------------------//
                                //DataType

                                if (syn.TypeData == "table")
                                {
                                    csvRow.Add("table");
                                }
                                else if (syn.TypeData == "column")
                                {
                                    csvRow.Add("column");
                                }
                                else if (syn.TypeData == "function")
                                {
                                    csvRow.Add("function");
                                }
                                else if (string.IsNullOrEmpty(syn.TypeData) == false)
                                {
                                    csvRow.Add(syn.TypeData);
                                }
                                else
                                {
                                    csvRow.Add("none");
                                }

                                //------------------------------//
                                //ExtendedType

                                if (syn.TypeData == "column")
                                {
                                    csvRow.Add(_keywordDataModel.StripJunk(syn.Parent.ToLower()));
                                }
                                else
                                {
                                    csvRow.Add("none");
                                }

                                //---------------------------------//
                                if (syn.OriginalName != null)
                                {
                                    csvRow.Add(syn.OriginalName);
                                }
                                else
                                {
                                    csvRow.Add("none");
                                }

                                csvRow.Add("1");

                                csvRow.Add(item.ToString());

                                writer.WriteRow(csvRow);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
               WriteToFileLog(ex.Message);
            }

            return fullPath;
        }

        private static bool WriteToDB(List<Synonym> synonymGroups, string path)
        {
            WriteToFileLog("WriteToDB");

            bool success = false;

            if (synonymGroups.Any())
            {
                System.Console.WriteLine("Import");

                var directoryOutput = ConfigurationManager.AppSettings["NLP_IMPPORT"];

                using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["PLATFORM-DATA"].ToString()))
                {
                    try
                    {
                        using (SqlCommand cmd = new SqlCommand("NLPHelper.proc_BulkInsertKeywordsNew", cn))
                        {
                            cmd.CommandTimeout = 0;
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@Path", path);

                            cn.Open();
                            cmd.ExecuteNonQuery();
                            cn.Close();

                            success = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        cn.Close();

                        WriteToFileLog(ex.ToString());

                        success = false;
                    }
                }
            }

            return success;
        }

        private static List<string> GetDataFromWordAPI(string searchWords)
        {
            var wordListings = new List<string>();

            try
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;


                var res = GetFromRestService("https://wordsapiv1.p.mashape.com/words/" + searchWords);
                dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(res);

                var resultsCount = (System.Collections.IList)json["results"];
                if (resultsCount != null)
                {
                    for (var i = 0; i < resultsCount.Count; i++)
                    {
                        var firstRCount = (System.Collections.IList)json["results"][i]["synonyms"];
                        var secondRCount = (System.Collections.IList)json["results"][i]["derivation"];

                        if (firstRCount != null)
                        {
                            for (var iv = 0; iv < firstRCount.Count; iv++)
                            {
                                var text = json["results"][i]["synonyms"][iv].ToString();
                                if (wordListings.Any(m => m == text) == false)
                                {
                                    wordListings.Add(text);
                                }
                            }
                        }

                        if (secondRCount != null)
                        {
                            for (var cc = 0; cc < secondRCount.Count; cc++)
                            {
                                var text = json["results"][i]["derivation"][cc].ToString();
                                if (wordListings.Any(m => m == text) == false)
                                {
                                    wordListings.Add(text);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                WriteToFileLog(e.Message);
            }
            return wordListings;
        }

        private static string GetFromRestService(string url)
        {
            try
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                System.Net.ServicePointManager.ServerCertificateValidationCallback 
                    = delegate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) 
                    { return true; };

                var apiUrl = url;
                WebRequest request = WebRequest.Create(apiUrl);
                // Set the Method property of the request to POST.
                request.Method = "GET";

                request.Headers.Add("X-Mashape-Key", "rAxpHYY11omshnDRGxOiYNpNgjb8p1d8Zt2jsnrCJiSPuH3NPk");


                // Set the ContentType property of the WebRequest.
                //request.ContentType = "application/x-www-form-urlencoded";
                request.ContentType = "application/json";


                // Get the response.
                WebResponse response = request.GetResponse();
                // Display the status.
                System.Console.WriteLine(((HttpWebResponse)response).StatusDescription);
                // Get the stream containing content returned by the server.
                var dataStream = response.GetResponseStream();
                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.
                string responseFromServer = reader.ReadToEnd();
                // Display the content.
                System.Console.WriteLine(responseFromServer);
                // Clean up the streams.
                reader.Close();
                response.Close();

                return responseFromServer;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
            }
            return "";
        }

        private static List<string> SearchEngineThesaurus(string searchWords, string searchEngineAPI)
        {
            var threshold = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["SEARCHENGINE_THESAURUS_THRESHOLD"].ToString());

            var candidateWords = new List<string>();
            var wordScoring = new Dictionary<string, double>();

            var getHtmlWeb = new HtmlWeb();
            try
            {
                if (string.IsNullOrEmpty(searchEngineAPI) == false)
                {
                    var query = searchEngineAPI + searchWords;
                    HtmlDocument document = getHtmlWeb.Load(query);
                    System.Console.WriteLine(query);

                    var eventHeadline = document.DocumentNode.SelectNodes("//b");

                    if (eventHeadline == null) eventHeadline = document.DocumentNode.SelectNodes("//strong");
                    var wordCheckingsList = new List<string>();
                    var totalWords = 0.0;
                    if (eventHeadline != null)
                    {
                        foreach (var item in eventHeadline)
                        {
                            var textExtract = item.InnerText.Trim().ToLower();

                            if (ContainsIrrelevantContent(textExtract) == false)
                            {
                                if (string.IsNullOrEmpty(textExtract) == false && textExtract != searchWords)
                                {
                                    var currentValue = 0.0;
                                    if (wordScoring.TryGetValue(textExtract, out currentValue))
                                    {
                                        wordScoring[textExtract] = wordScoring[textExtract] + 1;
                                    }
                                    else
                                    {
                                        wordScoring.Add(textExtract, 1);
                                    }
                                    totalWords++;
                                }
                            }
                        }
                    }

                    if (wordScoring.Any())
                    {
                        foreach (var item in wordScoring)
                        {
                            //var percentage = (item.Value / totalWords) * 100.0;
                            //if (percentage > threshold)
                            //{
                            //    candidateWords.Add(item.Key);
                            //}

                            if (item.Value > 1)
                            {
                                candidateWords.Add(item.Key);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //apply log4net
                ex.ToString();

                return candidateWords;
            }
            return candidateWords;
        }

        private static bool ContainsIrrelevantContent(string text)
        {
            foreach (var item in _illegals)
            {
                if (text.Contains(item)) return true;
            }
            double found = 0.0;
            if (Double.TryParse(text.Trim(), out found))
            {
                return true;
            }
            return false;
        }

        private static List<string> DictionaryWordsPastTenseEtc(string searchWords)
        {
            var totals = new List<string>();
            searchWords = searchWords.ToLower();

            var collection = _verbList.Where(mm => (mm.Verbn == searchWords)
            || (mm.Verbn == searchWords)
            || (mm.Present == searchWords)
            || (mm.Past == searchWords)
            || (mm.Past2nd == searchWords)
            || (mm.Plural == searchWords)
            || (mm.Plural2nd == searchWords)
            );

            if (collection != null && collection.Any())
            {
                totals.Add(collection.FirstOrDefault().Verbn);
                totals.Add(collection.FirstOrDefault().Present);
                totals.Add(collection.FirstOrDefault().Past);
                totals.Add(collection.FirstOrDefault().Past2nd);
                totals.Add(collection.FirstOrDefault().Plural);
                totals.Add(collection.FirstOrDefault().Plural2nd);
            }
            return totals;
        }

        private static List<string> LocalLookUpWords(string searchWords)
        {
            var candidateWords = new List<string>();

            try
            {
                var data = _aliasLookups.Where(mm => mm.Keyword == searchWords.ToLower());

                if (data.Any())
                {
                    candidateWords.AddRange(data.FirstOrDefault().Alias);
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
            return candidateWords;
        }


        private static List<string> DictionaryWords(string searchWords)
        {
            var candidateWords = new List<string>();
            var dictionary = System.Configuration.ConfigurationManager.AppSettings["DICTIONARY"].ToString();

            try
            {
                var getHtmlWeb = new HtmlWeb();

                if (string.IsNullOrEmpty(dictionary) == false)
                {
                    var query = dictionary + searchWords;
                    HtmlDocument document = getHtmlWeb.Load(query);
                    System.Console.WriteLine(query);

                    string classToFind = "luna-data-header";
                    var eventHeadline = document.DocumentNode.SelectNodes(string.Format("//*[contains(@class,'{0}')]", classToFind));
                    if (eventHeadline == null) eventHeadline = document.DocumentNode
                            .SelectNodes(string.Format("//*[contains(@class,'{0}')]", "relevancy-block"));

                    if (eventHeadline != null)
                    {
                        var aTag = eventHeadline.FirstOrDefault();
                        if (aTag.HasChildNodes)
                        {
                            var idToFind = "oneClick-link oneClick-available";
                            //Search subnodes :- https://stackoverflow.com/questions/6181014/html-agility-pack-problem-selecting-subnode


                            foreach (var abm in eventHeadline)
                            {
                                var words = abm.SelectNodes(".//span[@class='" + idToFind + "']");
                                if (words == null) words = abm.SelectNodes(".//span[@class='dbox-bold']");

                                if (words != null)
                                {
                                    foreach (var item in words)
                                    {
                                        var textExtract = item.InnerText.Trim();
                                        if (string.IsNullOrEmpty(textExtract) == false && candidateWords.Any(gn => gn == textExtract) == false)
                                        {
                                            var basic = RemoveTextExtras(textExtract.ToLower());
                                            if (candidateWords.Any(r => r == basic) == false)
                                            {
                                                candidateWords.Add(RemoveTextExtras(textExtract.ToLower()));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //apply log4net
                ex.ToString();

                return candidateWords;
            }
            return candidateWords;
        }

        private static string RemoveTextExtras(string text)
        {
            int founditem = text.IndexOf('?');
            string currentText = text;
            if (founditem > 0)
            {
                currentText = text.Replace('?', ' ');
                currentText = currentText.TrimEnd();
            }

            int bracketLeft = text.IndexOf(')');
            if (bracketLeft > 0)
            {
                var finalTxt = currentText.Replace(')', ' ');
                currentText = finalTxt.TrimEnd();
            }

            int bracketRight = text.IndexOf('(');
            if (bracketRight > 0)
            {
                var finalTxt = currentText.Replace('(', ' ');
                currentText = finalTxt.TrimEnd();
            }

            int founditemn = text.IndexOf(',');
            if (founditemn > 0)
            {
                var finalTxt = currentText.Replace(',', ' ');
                currentText = finalTxt.TrimEnd();
            }

            //commented out as this prevents "price changes of -1% type queries"
            int founditemk = text.IndexOf('-');
            if (founditemk > 0)
            {
                StringBuilder stringB = new StringBuilder(text);
                var totalIndex = founditemk + 2;

                var num = -1;

                var countLength = founditemk + 1;
                if (countLength < stringB.Length)
                {
                    if (int.TryParse(stringB[founditemk + 1].ToString(), out num) == false)
                    {
                        var finalTxt = currentText.Replace("-", " ");
                        currentText = finalTxt.TrimEnd();
                    }
                }
                else if (totalIndex < stringB.Length)
                {
                    if (int.TryParse(stringB[totalIndex].ToString(), out num) == false)
                    {
                        var finalTxt = currentText.Replace("-", " ");
                        currentText = finalTxt.TrimEnd();
                    }
                }
            }

            int founditembb = text.IndexOf(@"'s");
            if (founditembb > 0)
            {
                var finalTxt = currentText.Replace(@"'s", "");
                currentText = finalTxt.TrimEnd();
            }

            //apostrophe removal
            int founditemg = text.IndexOf(Convert.ToChar(8217));
            if (founditemg > 0)
            {
                var finalTxt = currentText.Replace(Convert.ToChar(8217), Convert.ToChar(32));
                currentText = finalTxt.TrimEnd();
            }

            int iter = 0;
            var rend = text.ToCharArray();
            for (int n = 0; n < rend.Count(); n++)
            {
                if ((rend[n] == '.'))
                {
                    var ith = n - 1;
                    if (ith < 0) ith = 0;

                    int tempint = 0;
                    if (int.TryParse(rend[ith].ToString(), out tempint) == false)
                    {
                        string find = rend[ith].ToString() + rend[n].ToString();

                        var finalTxt = currentText.Replace(find, rend[ith].ToString());
                        currentText = finalTxt.TrimEnd();
                    }
                }
                iter++;
            }
            return currentText;
        }

        private static List<string> ThesaurusWords(string searchWords)
        {
            // using System.Net;
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            // Use SecurityProtocolType.Ssl3 if needed for compatibility reasons

            var candidateWords = new List<string>();
            var thesaurus = System.Configuration.ConfigurationManager.AppSettings["THESAURUS"].ToString();

            try
            {
                var getHtmlWeb = new HtmlWeb();

                if (string.IsNullOrEmpty(thesaurus) == false)
                {
                    var query = thesaurus + searchWords;
                    HtmlDocument document = getHtmlWeb.Load(query);
                    System.Console.WriteLine(query);


                    string classToFind = "css-133coio";

                    var eventHeadline = document.DocumentNode
                        .SelectNodes(string.Format("//*[contains(@class,'{0}')]", classToFind));

                    if (eventHeadline != null)
                    {
                        var collection = new List<HtmlNode>();

                        if (eventHeadline.Count() > 6)
                        {
                            collection = eventHeadline.Take(7).ToList();
                        }
                        else
                        {
                            collection = eventHeadline.ToList();
                        }

                        foreach (var aTag in collection)
                        {
                            if (aTag.HasChildNodes)
                            {
                                candidateWords.Add(aTag.InnerText.ToLower());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //apply log4net
                ex.ToString();

                return candidateWords;
            }
            return candidateWords;
        }
    }         
}
