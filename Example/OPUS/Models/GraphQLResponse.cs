
namespace Example.OPUS.Models
{
    public class GraphQLResponse<T>
    {
        public T data { get; set; }

        public GraphQLError[] errors { get; set; }
    }
}
