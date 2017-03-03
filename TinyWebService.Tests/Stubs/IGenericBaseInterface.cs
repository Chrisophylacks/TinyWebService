using System;

namespace TinyWebService.Tests.Stubs
{
    public interface IGenericBaseInterface
    {
        T[] GetItems<T>();
    }

    public interface IGenericInterface
    {
        string[] GetItems();
    }

    public class GenericInterfaceImpl : IGenericBaseInterface, IGenericInterface
    {
        private readonly string[] _items;

        public GenericInterfaceImpl(string[] items)
        {
            _items = items;
        }

        T[] IGenericBaseInterface.GetItems<T>()
        {
            throw new Exception("generic call not allowed");
        }

        string[] IGenericInterface.GetItems()
        {
            return _items;
        }
    }
}