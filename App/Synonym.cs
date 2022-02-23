using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SynonymWebService
{
    public class Synonym
    {
        private string _searchWords;
        private string _suggestedPrimaryWords;
        private List<string> _synonymWords;

        public string TypeData { get; set; }
        public string Parent { get; set; }
        public string OriginalName { get; set; }


        public string SearchWords
        {
            get { return _searchWords; }
            set { _searchWords = value; }
        }

      
        public string SuggestedPrimaryWords
        {
            get { return _suggestedPrimaryWords; }
            set { _suggestedPrimaryWords = value; }
        }

      
        public List<string> SynonymWords
        {
            get { return _synonymWords; }
            set { _synonymWords = value; }
        }
    }
}