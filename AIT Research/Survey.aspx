<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Survey.aspx.cs" Inherits="AIT_Research.Survey" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Survey Question</title>
    <link href="Content/styles.css" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
        <div class="container">
            <div class="card">
                 <asp:Label ID="Warning" runat="server" Text="Please select an Option" Visible="False" CssClass="Warning"></asp:Label>
                 <h2>Survey Question</h2>
                 <asp:Label ID="queslbl" runat="server" CssClass="question" ></asp:Label>
                 <asp:RadioButtonList ID="RadioButtonListOptions" runat="server" CssClass="option-group" />
                 <asp:CheckBoxList ID="CheckBoxListOptions" runat="server" CssClass="option-group"> </asp:CheckBoxList>
                 <asp:TextBox ID="TextBox1" runat="server" CssClass="text-answer" Visible="false"></asp:TextBox>
                 <asp:TextBox ID="TextBoxInput" runat="server" CssClass="text-answer" Visible="false"></asp:TextBox> 

               
                
                <asp:Button ID="btnNext" runat="server" Text="Next" CssClass="btn" OnClick="btnNext_Click" />
            </div>
        </div>
    </form>
</body>
</html>