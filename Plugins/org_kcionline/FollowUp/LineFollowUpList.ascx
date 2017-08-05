<%@ Control Language="C#" AutoEventWireup="true" CodeFile="LineFollowUpList.ascx.cs" Inherits="org_kcionline.FollowUp.LineFollowUpList" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-star"></i> Cell Line Follow Ups</h1>
            </div>
            <div class="panel-body">

                <div class="grid grid-panel">
                    <Rock:Grid ID="gList" runat="server" AllowPaging="True" DataKeyNames="Id" OnRowSelected="gList_OnRowSelected" AllowSorting="true">
                        <Columns>
                            <Rock:RockBoundField DataField="Consolidator.FullName" HeaderText="Consolidator" SortExpression="Consolidator.FullName" />
                            <Rock:RockBoundField DataField="ConnectionRequest.PersonAlias.Person.FullName" HeaderText="Person" SortExpression="ConnectionRequest.PersonAlias.Person.FullName" />
                            <Rock:DateField DataField="ConnectionRequest.CreatedDateTime" HeaderText="Submitted" SortExpression="ConnectionRequest.CreatedDateTime" />
                            <Rock:RockBoundField DataField="ConnectionRequest.ConnectionStatus.Name" HeaderText="Status" SortExpression="ConnectionRequest.ConnectionStatus.Name" />
                        </Columns>
                    </Rock:Grid>
                </div>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
