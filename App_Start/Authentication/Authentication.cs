using BSS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;

public class Authentication : ApiController
{
    public Result ResultCheckToken;
    public UserToken UserToken = new UserToken();

    public Authentication()
    {
        ResultCheckToken = CacheUserToken.GetResultUserToken(out UserToken);
    }
}