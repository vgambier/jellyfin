﻿using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using ServiceStack.ServiceHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Api.UserLibrary
{
    /// <summary>
    /// Class GetPersons
    /// </summary>
    [Route("/Users/{UserId}/Items/{ParentId}/Persons", "GET")]
    [Route("/Users/{UserId}/Items/Root/Persons", "GET")]
    [Api(Description = "Gets all persons from a given item, folder, or the entire library")]
    public class GetPersons : GetItemsByName
    {
        /// <summary>
        /// Gets or sets the person types.
        /// </summary>
        /// <value>The person types.</value>
        public string PersonTypes { get; set; }
    }

    [Route("/Users/{UserId}/FavoritePersons/{Name}", "POST")]
    [Api(Description = "Marks a person as a favorite")]
    public class MarkFavoritePerson : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "Name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Name { get; set; }
    }

    [Route("/Users/{UserId}/FavoritePersons/{Name}", "DELETE")]
    [Api(Description = "Unmarks a person as a favorite")]
    public class UnmarkFavoritePerson : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "Name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Name { get; set; }
    }
    
    /// <summary>
    /// Class PersonsService
    /// </summary>
    public class PersonsService : BaseItemsByNameService<Person>
    {
        public PersonsService(IUserManager userManager, ILibraryManager libraryManager)
            : base(userManager, libraryManager)
        {
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetPersons request)
        {
            var result = GetResult(request).Result;

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(MarkFavoritePerson request)
        {
            var task = MarkFavorite(() => LibraryManager.GetPerson(request.Name), request.UserId, true);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(UnmarkFavoritePerson request)
        {
            var task = MarkFavorite(() => LibraryManager.GetPerson(request.Name), request.UserId, false);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Gets all items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{Tuple{System.StringFunc{System.Int32}}}.</returns>
        protected override IEnumerable<Tuple<string, Func<IEnumerable<BaseItem>>>> GetAllItems(GetItemsByName request, IEnumerable<BaseItem> items, User user)
        {
            var inputPersonTypes = ((GetPersons) request).PersonTypes;
            var personTypes = string.IsNullOrEmpty(inputPersonTypes) ? new string[] { } : inputPersonTypes.Split(',');

            var itemsList = items.Where(i => i.People != null).ToList();

            // Either get all people, or all people filtered by a specific person type
            var allPeople = GetAllPeople(itemsList, personTypes);

            return allPeople
                .Select(i => i.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)

                .Select(name => new Tuple<string, Func<IEnumerable<BaseItem>>>(name, () =>
                {
                    if (personTypes.Length == 0)
                    {
                        return itemsList.Where(i => i.People.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
                    }

                    return itemsList.Where(i => i.People.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && personTypes.Contains(p.Type ?? string.Empty, StringComparer.OrdinalIgnoreCase)));
                })
            );
        }

        /// <summary>
        /// Gets all people.
        /// </summary>
        /// <param name="itemsList">The items list.</param>
        /// <param name="personTypes">The person types.</param>
        /// <returns>IEnumerable{PersonInfo}.</returns>
        private IEnumerable<PersonInfo> GetAllPeople(IEnumerable<BaseItem> itemsList, string[] personTypes)
        {
            var people = itemsList.SelectMany(i => i.People.OrderBy(p => p.Type));


            return personTypes.Length == 0 ?

                people :

                people.Where(p => personTypes.Contains(p.Type ?? string.Empty, StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the entity.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{Genre}.</returns>
        protected override Task<Person> GetEntity(string name)
        {
            return LibraryManager.GetPerson(name);
        }
    }
}
