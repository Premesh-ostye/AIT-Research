﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AIT_Research
{
    public partial class StaffLogin1 : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void LoginBTN_Click(object sender, EventArgs e)
        {
            Response.Redirect("staffSearch.aspx");
        }
    }
}