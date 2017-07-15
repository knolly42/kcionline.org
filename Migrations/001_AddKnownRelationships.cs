using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock.Plugin;

namespace org.kcionline.bricksandmortarstudio.Migrations
{
    [MigrationNumber(1, "1.5.0")]
    class _001_AddKnownRelationships : Migration
    {
        public override void Up()
        {
            RockMigrationHelper.AddGroupTypeRole( "E0C5A0E2-B7B3-4EF4-820D-BBF7F9A374EF", "Consolidator", "A person that is the consolidator for the owner of this known relationship group", 0, null, null, "1bbe507b-a844-4815-8880-b4b4014929ce", false );
            RockMigrationHelper.AddGroupTypeRole( "E0C5A0E2-B7B3-4EF4-820D-BBF7F9A374EF", "Consolidated By", "A person that the owner of this known relationship group is being consolidated by", 0, null, null, "0bbee100-430d-472f-83a3-548680a14152", false );
        }

        public override void Down()
        {
            RockMigrationHelper.DeleteGroupTypeRole( "1bbe507b-a844-4815-8880-b4b4014929ce" );
            RockMigrationHelper.DeleteGroupTypeRole( "1bbe507b-a844-4815-8880-b4b4014929ce" );
        }
    }
}
