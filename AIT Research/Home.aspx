<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Home.aspx.cs" Inherits="AIT_Research.Home" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>AIT Research</title>
    <link href="Content/styles.css" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
        <div class="container">
            <div class="card">
                <h1>AIT <br />Research</h1>
                
                <div class="subtitle">Market Research Made Easy</div>
                <asp:Label ID="Warning" runat="server" Text="Warning" Visible="False" CssClass="Warning"></asp:Label>
                <div class="btnGroup">
                    <asp:Button ID="btnStartSurvey" runat="server" Text="Start Survey" CssClass="btn" OnClick="btnStartSurvey_Click" />
                    <asp:Button ID="btnStaffLogin" runat="server" Text="Login as Staff" CssClass="btn" OnClick="btnStaffLogin_Click" />
                </div>
            </div>
        </div>
    </form>
</body>
</html>