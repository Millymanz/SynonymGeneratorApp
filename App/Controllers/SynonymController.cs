using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Threading;

namespace SynonymWebService.Controllers
{
    public class SynonymController : ApiController
    {

        public void Get(string dagId)
        {
            Thread t = new Thread(new ParameterizedThreadStart(Run));
            t.Start(dagId);
        }       

        public static void Run(object dagId)
        {
            var dag = dagId.ToString();

            SynonymGenerator.GenerateSynonymsAutomatic(dag);

            //var keyword = dag;
            //var outcome = SynonymGenerator.GenerateSynonyms(keyword, false);

        }
    }
}
