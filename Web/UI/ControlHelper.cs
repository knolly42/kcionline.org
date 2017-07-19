﻿using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Web.UI.Controls;

namespace org.kcionline.bricksandmortarstudio.Web.UI
{
    internal static class ControlHelper
    {
        /// <summary>
        /// Inits the specified rock control.
        /// </summary>
        /// <param name="rockControl">The rock control.</param>
        public static void Init( IRockControl rockControl )
        {
            rockControl.RequiredFieldValidator = new RequiredFieldValidator();
            rockControl.HelpBlock = new HelpBlock();
            rockControl.WarningBlock = new WarningBlock();
        }

        /// <summary> 
        /// Creates the child controls and handles adding the required field validator control.
        /// </summary>
        /// <param name="rockControl">The rock control.</param>
        /// <param name="controls">The controls.</param>
        public static void CreateChildControls( IRockControl rockControl, ControlCollection controls )
        {
            if ( rockControl.RequiredFieldValidator != null )
            {
                rockControl.RequiredFieldValidator.ID = rockControl.ID + "_rfv";
                rockControl.RequiredFieldValidator.ControlToValidate = rockControl.ID;
                rockControl.RequiredFieldValidator.Display = ValidatorDisplay.Dynamic;
                rockControl.RequiredFieldValidator.CssClass = "validation-error help-inline";
                rockControl.RequiredFieldValidator.Enabled = rockControl.Required;
                controls.Add( rockControl.RequiredFieldValidator );
            }

            if ( rockControl.HelpBlock != null )
            {
                rockControl.HelpBlock.ID = rockControl.ID + "_hb";
                controls.Add( rockControl.HelpBlock );
            }

            if ( rockControl.WarningBlock != null )
            {
                rockControl.WarningBlock.ID = rockControl.ID + "_wb";
                controls.Add( rockControl.WarningBlock );
            }
        }

        /// <summary>
        /// Renders the control which handles adding all the IRockControl common pieces (Label, Help, etc.).
        /// </summary>
        /// <param name="rockControl">The rock control.</param>
        /// <param name="writer">The writer.</param>
        /// <param name="additionalCssClass">The additional CSS class.</param>
        public static void RenderControl( IRockControl rockControl, HtmlTextWriter writer, string additionalCssClass = "" )
        {
            bool renderLabel = ( !string.IsNullOrEmpty( rockControl.Label ) );
            bool renderHelp = ( rockControl.HelpBlock != null && !string.IsNullOrWhiteSpace( rockControl.Help ) );
            bool renderWarning = ( rockControl.WarningBlock != null && !string.IsNullOrWhiteSpace( rockControl.Warning ) );

            if ( renderLabel )
            {
                var cssClass = new StringBuilder();
                cssClass.AppendFormat( "form-group {0} {1}", rockControl.GetType().Name.SplitCase().Replace( ' ', '-' ).ToLower(), rockControl.FormGroupCssClass );
                if ( ( ( Control ) rockControl ).Page.IsPostBack && !rockControl.IsValid )
                {
                    cssClass.Append( " has-error" );
                }
                if ( rockControl.Required )
                {
                    if ( ( rockControl is IDisplayRequiredIndicator ) && !( rockControl as IDisplayRequiredIndicator ).DisplayRequiredIndicator )
                    {
                        // if this is a rock control that implements IDisplayRequiredIndicator and DisplayRequiredIndicator is false, don't add the " required " cssclass
                    }
                    else
                    {
                        cssClass.Append( " required" );
                    }
                }
                if ( !string.IsNullOrWhiteSpace( additionalCssClass ) )
                {
                    cssClass.Append( " " + additionalCssClass );
                }

                writer.AddAttribute( HtmlTextWriterAttribute.Class, cssClass.ToString() );
                writer.RenderBeginTag( HtmlTextWriterTag.Div );

                if ( !( rockControl is RockLiteral ) )
                {
                    writer.AddAttribute( HtmlTextWriterAttribute.Class, "control-label" );
                    writer.AddAttribute( HtmlTextWriterAttribute.For, rockControl.ClientID );
                }

                if ( rockControl is WebControl )
                {
                    // if the control has a Display Style, make sure the Label, Help, and Warning also get the same Display style
                    // For example, you might have rockControl.Style["Display"] = "none", so you probably want the label, help, and warning to also get not displayed
                    var rockControlDisplayStyle = ( rockControl as WebControl ).Style[HtmlTextWriterStyle.Display];
                    if ( rockControlDisplayStyle != null )
                    {
                        writer.AddStyleAttribute( HtmlTextWriterStyle.Display, rockControlDisplayStyle );
                        if ( rockControl.HelpBlock != null )
                        {
                            rockControl.HelpBlock.Style[HtmlTextWriterStyle.Display] = rockControlDisplayStyle;
                        }

                        if ( rockControl.WarningBlock != null )
                        {
                            rockControl.WarningBlock.Style[HtmlTextWriterStyle.Display] = rockControlDisplayStyle;
                        }
                    }
                }

                writer.RenderBeginTag( HtmlTextWriterTag.Label );
                writer.Write( rockControl.Label );

                if ( renderHelp )
                {
                    rockControl.HelpBlock.RenderControl( writer );
                }

                if ( renderWarning )
                {
                    rockControl.WarningBlock.RenderControl( writer );
                }

                writer.RenderEndTag();

                if ( rockControl is IRockControlAdditionalRendering )
                {
                    ( ( IRockControlAdditionalRendering ) rockControl ).RenderAfterLabel( writer );
                }

                writer.AddAttribute( HtmlTextWriterAttribute.Class, "control-wrapper" );
                writer.RenderBeginTag( HtmlTextWriterTag.Div );
            }

            rockControl.RenderBaseControl( writer );

            if ( renderLabel )
            {
                writer.RenderEndTag();
            }

            if ( !renderLabel && renderHelp )
            {
                rockControl.HelpBlock.RenderControl( writer );
            }

            if ( !renderLabel && renderWarning )
            {
                rockControl.WarningBlock.RenderControl( writer );
            }

            if ( rockControl.RequiredFieldValidator != null )
            {
                if ( rockControl.Required )
                {
                    rockControl.RequiredFieldValidator.Enabled = true;
                    if ( string.IsNullOrWhiteSpace( rockControl.RequiredFieldValidator.ErrorMessage ) )
                    {
                        rockControl.RequiredFieldValidator.ErrorMessage = rockControl.Label + " is Required.";
                    }
                    rockControl.RequiredFieldValidator.RenderControl( writer );
                }
                else
                {
                    rockControl.RequiredFieldValidator.Enabled = false;
                }
            }

            if ( renderLabel )
            {
                writer.RenderEndTag();
            }
        }

        public static void RenderControl( string label, Control control, HtmlTextWriter writer )
        {
            bool renderLabel = ( !string.IsNullOrEmpty( label ) );

            if ( renderLabel )
            {
                writer.AddAttribute( HtmlTextWriterAttribute.Class, "form-group" );
                writer.RenderBeginTag( HtmlTextWriterTag.Div );

                writer.AddAttribute( HtmlTextWriterAttribute.Class, "control-label" );
                writer.AddAttribute( HtmlTextWriterAttribute.For, control.ClientID );
                writer.RenderBeginTag( HtmlTextWriterTag.Label );
                writer.Write( label );
                writer.RenderEndTag();  // label

                control.RenderControl( writer );

                writer.AddAttribute( HtmlTextWriterAttribute.Class, "control-wrapper" );
                writer.RenderBeginTag( HtmlTextWriterTag.Div );
                control.RenderControl( writer );
                writer.RenderEndTag();

                writer.RenderEndTag();  // form-group
            }
            else
            {
                control.RenderControl( writer );
            }
        }
    }
}
