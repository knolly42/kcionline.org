using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;

namespace org.kcionline.bricksandmortarstudio.Workflow.Action
{
    [ActionCategory( "Connections" )]
    [Description( "Sets the connection request's connector." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Connection Request Connector Set" )]

    [WorkflowAttribute( "Connection Request Attribute", "The attribute that contains the connection request to add an activity to.", true, "", "", 0, null,
        new string[] { "Rock.Field.Types.ConnectionRequestFieldType" } )]
    [WorkflowAttribute( "Person Attribute", "An optional Person attribute that contains the person who is adding the activity.", true, "", "", 3, null,
        new string[] { "Rock.Field.Types.PersonFieldType" } )]
    public class SetConnectionRequestConnector : ActionComponent
    {
        public override bool Execute( RockContext rockContext, WorkflowAction action, Object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();
            var mergeFields = GetMergeFields( action );

            // Get the connection request
            ConnectionRequest request = null;
            var connectionRequestGuid = action.GetWorklowAttributeValue( GetAttributeValue( action, "ConnectionRequestAttribute" ).AsGuid() ).AsGuid();
            request = new ConnectionRequestService( rockContext ).Get( connectionRequestGuid );
            if ( request == null )
            {
                errorMessages.Add( "Invalid Connection Request Attribute or Value!" );
                return false;
            }

            // Get the connector
            PersonAlias personAlias = null;
            Guid? personAttributeGuid = GetAttributeValue( action, "PersonAttribute" ).AsGuidOrNull();
            if ( personAttributeGuid.HasValue )
            {
                Guid? personAliasGuid = action.GetWorklowAttributeValue( personAttributeGuid.Value ).AsGuidOrNull();
                if ( personAliasGuid.HasValue )
                {
                    personAlias = new PersonAliasService( rockContext ).Get( personAliasGuid.Value );
                    if ( personAlias == null )
                    {
                        errorMessages.Add( "Invalid Person" );
                        return false;
                    }
                }
            }

            request.ConnectorPersonAlias = personAlias;
            rockContext.SaveChanges();

            return true;
        }
    }
}
