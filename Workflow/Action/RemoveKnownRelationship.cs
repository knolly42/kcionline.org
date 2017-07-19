using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;

namespace org.kcionline.bricksandmortarstudio.Workflow.Action
{
    [ActionCategory( "People" )]
    [Description( "Removes a known relationship between two people." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Known Relationship Remove" )]

    [WorkflowAttribute( "Person", "Workflow attribute that contains the person to remove the relationship from.", true, "", "", 0, null,
        new[] { "Rock.Field.Types.PersonFieldType" } )]
    [GroupRoleField( "E0C5A0E2-B7B3-4EF4-820D-BBF7F9A374EF", "Relationship Type", "The type of known relationship that should be remove ", order:1 )]
    [WorkflowAttribute( "Relationship To", "Workflow attribute that contains the person that the relationship is with.", true, "", "", 2, null,
        new[] { "Rock.Field.Types.PersonFieldType" } )]
    public class RemoveKnownRelationship : ActionComponent
    {
        public override bool Execute( RockContext rockContext, WorkflowAction action, Object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            var personAliasService = new PersonAliasService( rockContext );

            #region Get attributes
            // get person
            Person person = null;
            var guidPersonAttribute = GetAttributeValue( action, "Person" ).AsGuidOrNull();
            if ( guidPersonAttribute.HasValue )
            {
                var attributePerson = AttributeCache.Read( guidPersonAttribute.Value, rockContext );
                if ( attributePerson != null )
                {
                    var attributePersonValue = action.GetWorklowAttributeValue( guidPersonAttribute.Value ).AsGuidOrNull();
                    if ( attributePersonValue.HasValue )
                    {
                        person = personAliasService.GetPerson( attributePersonValue.Value );
                        if ( person == null )
                        {
                            errorMessages.Add( string.Format( "Person could not be found for selected value ('{0}')!", guidPersonAttribute ) );
                            return false;
                        }
                    }
                }
            }

            Person relatedPerson = null;

            var guidRelatedPersonAttribute = GetAttributeValue( action, "RelationshipTo" ).AsGuidOrNull();
            if ( guidRelatedPersonAttribute.HasValue )
            {
                var attributePerson = AttributeCache.Read( guidRelatedPersonAttribute.Value, rockContext );
                if ( attributePerson != null )
                {
                    var attributePersonValue = action.GetWorklowAttributeValue( guidRelatedPersonAttribute.Value ).AsGuidOrNull();
                    if ( attributePersonValue.HasValue )
                    {
                        relatedPerson = personAliasService.GetPerson( attributePersonValue.Value );
                        if ( relatedPerson == null )
                        {
                            errorMessages.Add( string.Format( "Person could not be found for selected value ('{0}')!", guidPersonAttribute ) );
                            return false;
                        }
                    }
                }
            }


            var groupTypeRoleService = new GroupTypeRoleService( rockContext );

            GroupTypeRole relationshipType = null;
            var relationshipRoleGuid = GetActionAttributeValue( action, "RelationshipType" ).AsGuidOrNull();
            if ( relationshipRoleGuid.HasValue )
            {
                relationshipType = groupTypeRoleService.Get( relationshipRoleGuid.Value );
                if ( relationshipType == null )
                {
                    errorMessages.Add( string.Format( "GroupTypeRole (Relationship Type) could not be found for selected value ('{0}')!", relationshipRoleGuid ) );
                    return false;
                }
            }
            #endregion

            var groupMemberService = new GroupMemberService( rockContext );

            // Check if relationship already exists
            if ( !groupMemberService.GetKnownRelationship( person.Id, relationshipType.Id )
                                  .Any( gm => gm.Person.Id == relatedPerson.Id ) )
            {
                errorMessages.Add( string.Format( "Relationship of {0} doesn't exist between {1} and {2}", relationshipType.Name, person.FullName, relatedPerson.FullName ) );
                return false;
            }

            groupMemberService.DeleteKnownRelationship( person.Id, relatedPerson.Id, relationshipType.Id );

            // Remove inverse relationship if it exists.
            if ( relationshipType.Attributes.ContainsKey( "InverseRelationship" ) )
            {
                var inverseRelationshipTypeGuid =
                    relationshipType.GetAttributeValue( "InverseRelationship" ).AsGuidOrNull();
                if ( inverseRelationshipTypeGuid.HasValue )
                {
                    var inverseRelationshipType = groupTypeRoleService.Get( inverseRelationshipTypeGuid.Value );
                    // Ensure relationship doesn't already exist
                    if ( groupMemberService.GetKnownRelationship( relatedPerson.Id, inverseRelationshipType.Id )
                                          .Any( gm => gm.Person.Id == person.Id ) )
                    {
                        groupMemberService.DeleteKnownRelationship( relatedPerson.Id, person.Id, inverseRelationshipType.Id );
                    }

                }
            }


            return true;
        }
    }
}