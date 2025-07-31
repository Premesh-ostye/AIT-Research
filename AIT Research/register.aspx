<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="register.aspx.cs" Inherits="AIT_Research.Register" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Registration Page</title>
    <link href="Content/styles.css" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
        <div class="container">
            <div class="card">
                <h1>Register</h1>

                <asp:Label ID="lblMsg" runat="server" ForeColor="Red" />

                <div class="textFieldGRP">
                    <asp:TextBox ID="inputFN" runat="server" CssClass="text-answer" placeholder="First Name" />
                    <asp:TextBox ID="txtLastName" runat="server" CssClass="text-answer" placeholder="Last Name" />
                    <asp:TextBox ID="txtDOB" runat="server" TextMode="Date" CssClass="text-answer" placeholder="Date of Birth" />
                    <asp:TextBox ID="txtPhone" runat="server" TextMode="Phone" CssClass="text-answer" placeholder="Phone Number" />
                </div>

                <asp:Button ID="Submitbtn" runat="server" Text="Register" CssClass="btn" OnClick="Submitbtn_Click" />
            </div>
        </div>
    </form>
</body>
</html>
