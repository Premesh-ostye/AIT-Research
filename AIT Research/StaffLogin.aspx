<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="StaffLogin.aspx.cs" Inherits="AIT_Research.StaffLogin1" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Staff Login</title>
    <link href="Content/styles.css" rel="stylesheet" type="text/css" />

</head>
<body>
    <form id="form1" runat="server">
              <div class="container">
    <div class="card">
        <h1>Welcome <br /> Login</h1>
         <div class="textFieldGRP">
             <input id="inputUN" type="text" class="text-answer" placeholder="UserName" />
             <input id="inputPW" type="password" class="text-answer" placeholder="Password"  />
          
          </div>
            <asp:Button ID="LoginBTN" runat="server" Text="Login" CssClass="btn" />
        </div>
   
</div>
    </form>
</body>
</html>
