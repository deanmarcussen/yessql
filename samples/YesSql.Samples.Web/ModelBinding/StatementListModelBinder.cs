using Microsoft.AspNetCore.Mvc.ModelBinding;
using Parlot.Fluent;
using System;
using System.Threading.Tasks;

namespace YesSql.Search.ModelBinding
{
    public class StatementListModelBinder : IModelBinder
    {
        private readonly ISearchParser _searchParser;

        public StatementListModelBinder(ISearchParser searchParser)
        {
            _searchParser = searchParser;
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var modelName = bindingContext.ModelName;

            // Try to fetch the value of the argument by name q=
            var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

            if (valueProviderResult == ValueProviderResult.None)
            {
                bindingContext.Result = ModelBindingResult.Success(new StatementList());

                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

            var value = valueProviderResult.FirstValue;

            // Check if the argument value is null or empty
            if (string.IsNullOrEmpty(value))
            {
                bindingContext.Result = ModelBindingResult.Success(new StatementList());
                
                return Task.CompletedTask;
            }

            var statementList = _searchParser.ParseSearch(value);

            bindingContext.Result = ModelBindingResult.Success(statementList);
            
            return Task.CompletedTask;
        }
    }
}