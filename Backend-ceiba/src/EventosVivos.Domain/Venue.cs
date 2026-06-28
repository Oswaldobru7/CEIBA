namespace EventosVivos.Domain;

public sealed class Venue
{
    public Venue(int id, string name, int capacity, string city)
    {
        Id = id;
        Name = name;
        Capacity = capacity;
        City = city;
    }

    public int Id { get; }
    public string Name { get; }
    public int Capacity { get; }
    public string City { get; }
}
