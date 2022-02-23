using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;



namespace SynonymWebService
{
    public class DataModel
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        public List<string> GetWordContains()
        {
            var collection = new List<string>();

            try
            {

                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["NLP-DATA"].ToString()))
                {
                    var sqlCommand = new SqlCommand("proc_GetWordContains", con);

                    con.Open();

                    sqlCommand.CommandType = CommandType.StoredProcedure;

                    SqlDataReader rdr = sqlCommand.ExecuteReader();

                    var previous = string.Empty;

                    while (rdr.Read())
                    {
                        collection.Add(rdr["Keyword"].ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

            return collection;
        }



        public List<Synonym> GetDefaultTypesData()
        {
            var collection = new List<Synonym>();

            try
            {

                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["NLP-DATA"].ToString()))
                {
                    var sqlCommand = new SqlCommand("proc_GetDefaultKeywords", con);

                    con.Open();

                    sqlCommand.CommandType = CommandType.StoredProcedure;

                    SqlDataReader rdr = sqlCommand.ExecuteReader();

                    var previous = string.Empty;

                    while (rdr.Read())
                    {
                        var currentKeyword = rdr["Keyword"].ToString();

                        if (previous != currentKeyword)
                        {
                            var syn = new Synonym();
                            syn.SuggestedPrimaryWords = StripJunk(currentKeyword);
                            syn.TypeData = rdr["DataType"].ToString();
                            syn.SynonymWords = new List<string>();
                            syn.Parent = rdr["TRTokenType"].ToString();
                            collection.Add(syn);
                        }

                        collection.LastOrDefault().SynonymWords.Add(rdr["KeywordAlias"].ToString());

                        previous = currentKeyword;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

            return collection;
        }

        public List<Synonym>  GetDatasetMetaData(string dagId)
        {
            var collection = new List<Synonym>();

            try
            {

                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["PLATFORM-DATA"].ToString()))
                {
                    var sqlCommand = new SqlCommand("proc_GetTableset", con);
                    sqlCommand.Parameters.AddWithValue("@DagId", dagId);

                    con.Open();

                    sqlCommand.CommandType = CommandType.StoredProcedure;

                    SqlDataReader rdr = sqlCommand.ExecuteReader();

                    var previous = string.Empty;

                    while (rdr.Read())
                    {
                        var currentTable = rdr["TableName"].ToString();

                        if (previous != currentTable)
                        {
                            var syn = new Synonym();
                            syn.SuggestedPrimaryWords = StripJunk(currentTable);
                            syn.TypeData = "table";
                            syn.OriginalName = rdr["TableName"].ToString();

                            collection.Add(syn);
                        }

                        var syndata = new Synonym();
                        syndata.SuggestedPrimaryWords = StripJunk(rdr["ColumnName"].ToString());
                        syndata.TypeData = "column";
                        syndata.Parent = currentTable;
                        syndata.OriginalName = rdr["ColumnName"].ToString();

                        collection.Add(syndata);

                        previous = currentTable;
                    }                   
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

            return collection;
        }

        public string StripJunk(string text)
        {
            var array = text.Split('.');

            if (array.Count() > 1)
            {
                return array.LastOrDefault();
            }
            else
            {
                array = text.Split('_');

                if (array.Count() > 1)
                {
                    return array.LastOrDefault();
                }
            }

            return text;
        }
    }
}
