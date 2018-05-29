using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Rest.Controllers;
using Rock.Rest.Filters;
using Rock.Web.Cache;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.IO;



namespace org.kcionline.bricksandmortarstudio.Rest
{
    public class LinePeopleController : Rock.Rest.ApiController<Person>
    {
        public LinePeopleController() : base(new PersonService(new RockContext())) { }

        /// <summary>
        /// Returns results to the Person Picker
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="includeHtml">if set to <c>true</c> [include HTML].</param>
        /// <param name="includeDetails">if set to <c>true</c> [include details].</param>
        /// <param name="includeBusinesses">if set to <c>true</c> [include businesses].</param>
        /// <param name="includeDeceased">if set to <c>true</c> [include deceased].</param>
        /// <returns></returns>
        [Authenticate, Secured]
        [HttpGet]
        [System.Web.Http.Route("api/com_bricksandmortarstudio/LineSearch")]
        public IQueryable<PersonSearchResult> Search(string name, bool includeHtml, bool includeDetails, bool includeBusinesses = false, bool includeDeceased = false)
        {
            int count = 20;
            bool showFullNameReversed;
            bool allowFirstNameOnly = false;

            var searchComponent = Rock.Search.SearchContainer.GetComponent(typeof(Rock.Search.Person.Name).FullName);
            if (searchComponent != null)
            {
                allowFirstNameOnly = searchComponent.GetAttributeValue("FirstNameSearch").AsBoolean();
            }

            // Get the line follow-ups for the current user
            var rockContext = this.Service.Context as Rock.Data.RockContext;
            var allowedPersons = Utils.LineQuery.GetPeopleInLine(this.Service as PersonService, GetPerson(), rockContext, true);

            //var activeRecordStatusValue = CacheDefinedValue.Get(SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid());
            var activeRecordStatusValue = DefinedValueCache.Read(Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid());
            int activeRecordStatusValueId = activeRecordStatusValue != null ? activeRecordStatusValue.Id : 0;

            IQueryable<Person> sortedPersonQry = (this.Service as PersonService)
                .GetByFullNameOrdered(name, true, includeBusinesses, allowFirstNameOnly, out showFullNameReversed)
                .Take(count)
                 .Where(sp => sp.Id > 0 && allowedPersons.Select(ap => ap.Id).Contains(sp.Id));

            if (includeDetails == false)
            {
                var personService = this.Service as PersonService;

                var simpleResult = sortedPersonQry.AsNoTracking().ToList().Select(a => new PersonSearchResult
                {
                    Id = a.Id,
                    Name = showFullNameReversed
                   ? Person.FormatFullNameReversed(a.LastName, a.NickName, a.SuffixValueId, a.RecordTypeValueId)
                   : Person.FormatFullName(a.NickName, a.LastName, a.SuffixValueId, a.RecordTypeValueId),
                    IsActive = a.RecordStatusValueId.HasValue && a.RecordStatusValueId == activeRecordStatusValueId,
                    //RecordStatus = a.RecordStatusValueId.HasValue ? CacheDefinedValue.Get(a.RecordStatusValueId.Value).Value : string.Empty,
                    RecordStatus = a.RecordStatusValueId.HasValue ? DefinedValueCache.Read(a.RecordStatusValueId.Value).Value : string.Empty,

                    Age = Person.GetAge(a.BirthDate) ?? -1,
                    FormattedAge = a.FormatAge(),
                    SpouseName = personService.GetSpouse(a, x => x.Person.NickName)
                });

                return simpleResult.AsQueryable();
            }
            else
            {
                List<PersonSearchResult> searchResult = SearchWithDetails(sortedPersonQry, showFullNameReversed);
                return searchResult.AsQueryable();
            }
        }



        /// <summary>
        /// Returns a List of PersonSearchRecord based on the sorted person query
        /// </summary>
        /// <param name="sortedPersonQry">The sorted person qry.</param>
        /// <param name="showFullNameReversed">if set to <c>true</c> [show full name reversed].</param>
        /// <returns></returns>
        private List<PersonSearchResult> SearchWithDetails(IQueryable<Person> sortedPersonQry, bool showFullNameReversed)
        {
            var rockContext = this.Service.Context as Rock.Data.RockContext;
            var phoneNumbersQry = new PhoneNumberService(rockContext).Queryable();
            var sortedPersonList = sortedPersonQry.Include(a => a.PhoneNumbers).AsNoTracking().ToList();
            Guid activeRecord = new Guid(Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE);

            List<PersonSearchResult> searchResult = new List<PersonSearchResult>();
            foreach (var person in sortedPersonList)
            {
                PersonSearchResult personSearchResult = new PersonSearchResult();
                personSearchResult.Id = person.Id;
                personSearchResult.Name = showFullNameReversed ? person.FullNameReversed : person.FullName;
                if (person.RecordStatusValueId.HasValue)
                {
                    var recordStatus = DefinedValueCache.Read(person.RecordStatusValueId.Value);
                    personSearchResult.RecordStatus = recordStatus.Value;
                    personSearchResult.IsActive = recordStatus.Guid.Equals(activeRecord);
                }
                else
                {
                    personSearchResult.RecordStatus = string.Empty;
                    personSearchResult.IsActive = false;
                }

                GetPersonSearchDetails(personSearchResult, person);

                searchResult.Add(personSearchResult);
            }

            return searchResult;
        }

        /// <summary>
        /// Gets the person search details.
        /// </summary>
        /// <param name="personSearchResult">The person search result.</param>
        /// <param name="person">The person.</param>
        private void GetPersonSearchDetails(PersonSearchResult personSearchResult, Person person)
        {
            var rockContext = this.Service.Context as Rock.Data.RockContext;

            var appPath = System.Web.VirtualPathUtility.ToAbsolute("~");
            string itemDetailFormat = @"
<div class='picker-select-item-details clearfix' style='display: none;'>
	{0}
	<div class='contents'>
        {1}
	</div>
</div>
";

            var familyGroupType = GroupTypeCache.Read(Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY.AsGuid());
            int adultRoleId = familyGroupType.Roles.First(a => a.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid()).Id;

            int groupTypeFamilyId = GroupTypeCache.Read(Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY.AsGuid()).Id;

            // figure out Family, Address, Spouse
            GroupMemberService groupMemberService = new GroupMemberService(rockContext);

            Guid? recordTypeValueGuid = null;
            if (person.RecordTypeValueId.HasValue)
            {
                recordTypeValueGuid = DefinedValueCache.Read(person.RecordTypeValueId.Value).Guid;
            }

            personSearchResult.ImageHtmlTag = Person.GetPersonPhotoImageTag(person, 50, 50);
            personSearchResult.Age = person.Age.HasValue ? person.Age.Value : -1;
            personSearchResult.ConnectionStatus = person.ConnectionStatusValueId.HasValue ? DefinedValueCache.Read(person.ConnectionStatusValueId.Value).Value : string.Empty;
            personSearchResult.Gender = person.Gender.ConvertToString();
            personSearchResult.Email = person.Email;

            string imageHtml = string.Format(
                "<div class='person-image' style='background-image:url({0}&width=65);background-size:cover;background-position:50%'></div>",
                Person.GetPersonPhotoUrl(person, 200, 200));

            string personInfoHtml = string.Empty;
            Guid matchLocationGuid;
            bool isBusiness;
            if (recordTypeValueGuid.HasValue && recordTypeValueGuid == Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_BUSINESS.AsGuid())
            {
                isBusiness = true;
                matchLocationGuid = Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_WORK.AsGuid();
            }
            else
            {
                isBusiness = false;
                matchLocationGuid = Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid();
            }

            var familyGroupMember = groupMemberService.Queryable()
                .Where(a => a.PersonId == person.Id)
                .Where(a => a.Group.GroupTypeId == groupTypeFamilyId)
                .OrderBy(a => a.PersonId)
                .Select(s => new
                {
                    s.GroupRoleId,
                    GroupLocation = s.Group.GroupLocations.Where(a => a.GroupLocationTypeValue.Guid == matchLocationGuid).Select(a => a.Location).FirstOrDefault()
                }).FirstOrDefault();

            int? personAge = person.Age;

            if (familyGroupMember != null)
            {
                if (isBusiness)
                {
                    personInfoHtml += "Business";
                }
                else
                {
                    personInfoHtml += "<div class='role'>" + familyGroupType.Roles.First(a => a.Id == familyGroupMember.GroupRoleId).Name + "</div>";
                }

                if (personAge != null)
                {
                    personInfoHtml += " <em class='age'>(" + person.FormatAge() + " old)</em>";
                }

                if (familyGroupMember.GroupRoleId == adultRoleId)
                {
                    var personService = this.Service as PersonService;
                    var spouse = personService.GetSpouse(person, a => new
                    {
                        a.Person.NickName,
                        a.Person.LastName,
                        a.Person.SuffixValueId
                    });

                    if (spouse != null)
                    {
                        string spouseFullName = Person.FormatFullName(spouse.NickName, spouse.LastName, spouse.SuffixValueId);
                        personInfoHtml += "<p class='spouse'><strong>Spouse:</strong> " + spouseFullName + "</p>";
                        personSearchResult.SpouseName = spouseFullName;
                    }
                }
            }
            else
            {
                if (personAge != null)
                {
                    personInfoHtml += personAge.ToString() + " yrs old";
                }
            }

            if (familyGroupMember != null)
            {
                var location = familyGroupMember.GroupLocation;

                if (location != null)
                {
                    string addressHtml = "<div class='address'><h5>Address</h5>" + location.GetFullStreetAddress().ConvertCrLfToHtmlBr() + "</div>";
                    personSearchResult.Address = location.GetFullStreetAddress();
                    personInfoHtml += addressHtml;
                }
            }

            // Generate the HTML for Email and PhoneNumbers
            if (!string.IsNullOrWhiteSpace(person.Email) || person.PhoneNumbers.Any())
            {
                string emailAndPhoneHtml = "<div class='margin-t-sm'>";
                emailAndPhoneHtml += "<span class='email'>" + person.Email + "</span>";
                string phoneNumberList = "<div class='phones'>";
                foreach (var phoneNumber in person.PhoneNumbers)
                {
                    var phoneType = DefinedValueCache.Read(phoneNumber.NumberTypeValueId ?? 0);
                    phoneNumberList += string.Format(
                        "<br>{0} <small>{1}</small>",
                        phoneNumber.IsUnlisted ? "Unlisted" : phoneNumber.NumberFormatted,
                        phoneType != null ? phoneType.Value : string.Empty);
                }

                emailAndPhoneHtml += phoneNumberList + "</div></div>";

                personInfoHtml += emailAndPhoneHtml;
            }

            // force the link to open a new scrollable,resizable browser window (and make it work in FF, Chrome and IE) http://stackoverflow.com/a/2315916/1755417
            personInfoHtml += $"<p class='margin-t-sm'><small><a class='cursor-pointer' onclick=\"javascript: window.open('/person/{person.Id}', '_blank', 'scrollbars=1,resizable=1,toolbar=1'); return false;\" data-toggle=\"tooltip\" title=\"View Profile\">View Profile</a></small></p>";

            personSearchResult.PickerItemDetailsImageHtml = imageHtml;
            personSearchResult.PickerItemDetailsPersonInfoHtml = personInfoHtml;
            personSearchResult.PickerItemDetailsHtml = string.Format(itemDetailFormat, imageHtml, personInfoHtml);
        }

        /// <summary>
        ///
        /// </summary>
        public class PersonSearchResult
        {
            /// <summary>
            /// Gets or sets the id.
            /// </summary>
            /// <value>
            /// The id.
            /// </value>
            public int Id { get; set; }

            /// <summary>
            /// Gets or sets the full name last first.
            /// </summary>
            /// <value>
            /// The full name last first.
            /// </value>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether this instance is active.
            /// </summary>
            /// <value>
            ///   <c>true</c> if this instance is active; otherwise, <c>false</c>.
            /// </value>
            public bool IsActive { get; set; }

            /// <summary>
            /// Gets or sets the image HTML tag.
            /// </summary>
            /// <value>
            /// The image HTML tag.
            /// </value>
            public string ImageHtmlTag { get; set; }

            /// <summary>
            /// Gets or sets the age in years
            /// NOTE: returns -1 if age is unknown
            /// </summary>
            /// <value>The age.</value>
            public int Age { get; set; }

            /// <summary>
            /// Gets or sets the formatted age.
            /// </summary>
            /// <value>
            /// The formatted age.
            /// </value>
            public string FormattedAge { get; set; }

            /// <summary>
            /// Gets or sets the gender.
            /// </summary>
            /// <value>The gender.</value>
            public string Gender { get; set; }

            /// <summary>
            /// Gets or sets the connection status.
            /// </summary>
            /// <value>The connection status.</value>
            public string ConnectionStatus { get; set; }

            /// <summary>
            /// Gets or sets the record status.
            /// </summary>
            /// <value>The member status.</value>
            public string RecordStatus { get; set; }

            /// <summary>
            /// Gets or sets the email.
            /// </summary>
            /// <value>
            /// The email.
            /// </value>
            public string Email { get; set; }

            /// <summary>
            /// Gets or sets the name of the spouse.
            /// </summary>
            /// <value>
            /// The name of the spouse.
            /// </value>
            public string SpouseName { get; set; }

            /// <summary>
            /// Gets or sets the address.
            /// </summary>
            /// <value>
            /// The address.
            /// </value>
            public string Address { get; set; }

            /// <summary>
            /// Gets or sets the picker item details HTML.
            /// </summary>
            /// <value>
            /// The picker item details HTML.
            /// </value>
            public string PickerItemDetailsHtml { get; set; }

            /// <summary>
            /// Gets or sets the picker item details image HTML.
            /// </summary>
            /// <value>
            /// The picker item details image HTML.
            /// </value>
            public string PickerItemDetailsImageHtml { get; set; }

            /// <summary>
            /// Gets or sets the picker item details person information HTML.
            /// </summary>
            /// <value>
            /// The picker item details person information HTML.
            /// </value>
            public string PickerItemDetailsPersonInfoHtml { get; set; }
        }
    }
}
