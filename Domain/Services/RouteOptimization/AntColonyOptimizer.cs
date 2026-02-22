// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Services.RouteOptimization;

public class AntColonyOptimizer
{
    private readonly DistanceMatrix _distanceMatrix;
    private readonly ILogger _logger;
    private readonly Random _random = new();

    private const int ANT_COUNT = 100;
    private const int MAX_ITERATIONS = 200;
    private const double ALPHA = 1.0;
    private const double BETA = 5.0;
    private const double EVAPORATION_RATE = 0.5;
    private const double PHEROMONE_CONSTANT = 100.0;
    private const double INITIAL_PHEROMONE = 1.0;

    private double[,] _pheromones;
    private int _cityCount;

    public AntColonyOptimizer(DistanceMatrix distanceMatrix, ILogger logger)
    {
        _distanceMatrix = distanceMatrix;
        _logger = logger;
        _cityCount = distanceMatrix.Locations.Count;
        _pheromones = new double[_cityCount, _cityCount];
        InitializePheromones();
    }

    public List<int> FindOptimalRoute(int? fixedStart = null, int? fixedEnd = null)
    {
        bool isRoundTrip = fixedStart.HasValue && fixedEnd.HasValue && fixedStart.Value == fixedEnd.Value;

        _logger.LogInformation(
            "Starting ACO optimization with {AntCount} ants, {Iterations} iterations, isRoundTrip={IsRoundTrip}",
            ANT_COUNT, MAX_ITERATIONS, isRoundTrip);

        List<int>? bestRoute = null;
        double bestDistance = double.MaxValue;

        for (int iteration = 0; iteration < MAX_ITERATIONS; iteration++)
        {
            var ants = new List<Ant>();

            for (int i = 0; i < ANT_COUNT; i++)
            {
                var ant = new Ant(_cityCount, fixedStart, fixedEnd, _random);
                ant.ConstructSolution(_distanceMatrix.Matrix, _pheromones, ALPHA, BETA);
                ants.Add(ant);

                var distance = CalculateTourDistance(ant.Route, isRoundTrip);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestRoute = new List<int>(ant.Route);
                    _logger.LogInformation(
                        "Iteration {Iteration}: New best distance = {Distance:F2} km",
                        iteration, bestDistance);
                }
            }

            UpdatePheromones(ants, isRoundTrip);
        }

        _logger.LogInformation("ACO completed. Best distance: {Distance:F2} km", bestDistance);

        if (bestRoute != null)
        {
            bestRoute = Apply2Opt(bestRoute, fixedStart, fixedEnd, isRoundTrip);
            var improvedDistance = CalculateTourDistance(bestRoute, isRoundTrip);
            _logger.LogInformation("After 2-opt optimization: {Distance:F2} km", improvedDistance);
        }

        return bestRoute ?? Enumerable.Range(0, _cityCount).ToList();
    }

    private List<int> Apply2Opt(List<int> route, int? fixedStart, int? fixedEnd, bool isRoundTrip)
    {
        var improved = true;
        var bestRoute = new List<int>(route);
        var bestDistance = CalculateTourDistance(bestRoute, isRoundTrip);

        int startIndex = fixedStart.HasValue ? 1 : 0;
        int endIndex = bestRoute.Count - 1;

        while (improved)
        {
            improved = false;

            for (int i = startIndex; i < endIndex - 1; i++)
            {
                for (int j = i + 1; j <= endIndex; j++)
                {
                    var newRoute = TwoOptSwap(bestRoute, i, j);
                    var newDistance = CalculateTourDistance(newRoute, isRoundTrip);

                    if (newDistance < bestDistance - 0.001)
                    {
                        bestRoute = newRoute;
                        bestDistance = newDistance;
                        improved = true;
                        _logger.LogDebug("2-opt improvement: swapped {I}-{J}, new distance: {Distance:F2} km", i, j, newDistance);
                    }
                }
            }
        }

        return bestRoute;
    }

    private List<int> TwoOptSwap(List<int> route, int i, int j)
    {
        var newRoute = new List<int>();

        for (int k = 0; k < i; k++)
        {
            newRoute.Add(route[k]);
        }

        for (int k = j; k >= i; k--)
        {
            newRoute.Add(route[k]);
        }

        for (int k = j + 1; k < route.Count; k++)
        {
            newRoute.Add(route[k]);
        }

        return newRoute;
    }

    private void InitializePheromones()
    {
        for (int i = 0; i < _cityCount; i++)
        {
            for (int j = 0; j < _cityCount; j++)
            {
                _pheromones[i, j] = INITIAL_PHEROMONE;
            }
        }
    }

    private void UpdatePheromones(List<Ant> ants, bool isRoundTrip)
    {
        for (int i = 0; i < _cityCount; i++)
        {
            for (int j = 0; j < _cityCount; j++)
            {
                _pheromones[i, j] *= (1.0 - EVAPORATION_RATE);
            }
        }

        foreach (var ant in ants)
        {
            var tourDistance = CalculateTourDistance(ant.Route, isRoundTrip);
            var pheromoneDeposit = PHEROMONE_CONSTANT / tourDistance;

            for (int i = 0; i < ant.Route.Count - 1; i++)
            {
                var from = ant.Route[i];
                var to = ant.Route[i + 1];
                _pheromones[from, to] += pheromoneDeposit;
                _pheromones[to, from] += pheromoneDeposit;
            }

            if (isRoundTrip && ant.Route.Count > 0)
            {
                var lastCity = ant.Route.Last();
                var firstCity = ant.Route.First();
                _pheromones[lastCity, firstCity] += pheromoneDeposit;
                _pheromones[firstCity, lastCity] += pheromoneDeposit;
            }
        }
    }

    private double CalculateTourDistance(List<int> route, bool isRoundTrip = false)
    {
        double total = 0.0;
        for (int i = 0; i < route.Count - 1; i++)
        {
            total += _distanceMatrix.Matrix[route[i], route[i + 1]];
        }

        if (isRoundTrip && route.Count > 0)
        {
            total += _distanceMatrix.Matrix[route.Last(), route.First()];
        }

        return total;
    }

    private class Ant
    {
        public List<int> Route { get; }
        private readonly HashSet<int> _visited;
        private readonly int _cityCount;
        private readonly int? _fixedStart;
        private readonly int? _fixedEnd;
        private readonly Random _random;

        public Ant(int cityCount, int? fixedStart, int? fixedEnd, Random random)
        {
            _cityCount = cityCount;
            _fixedStart = fixedStart;
            _fixedEnd = fixedEnd;
            _random = random;
            Route = new List<int>();
            _visited = new HashSet<int>();
        }

        public void ConstructSolution(double[,] distances, double[,] pheromones, double alpha, double beta)
        {
            int currentCity;
            bool isRoundTrip = _fixedStart.HasValue && _fixedEnd.HasValue && _fixedStart.Value == _fixedEnd.Value;

            if (_fixedStart.HasValue)
            {
                currentCity = _fixedStart.Value;
            }
            else
            {
                currentCity = _random.Next(_cityCount);
            }

            Route.Add(currentCity);
            _visited.Add(currentCity);

            while (_visited.Count < _cityCount)
            {
                var nextCity = SelectNextCity(currentCity, distances, pheromones, alpha, beta);
                Route.Add(nextCity);
                _visited.Add(nextCity);
                currentCity = nextCity;
            }

            if (_fixedEnd.HasValue && !isRoundTrip && Route.Last() != _fixedEnd.Value)
            {
                var endIndex = Route.IndexOf(_fixedEnd.Value);
                if (endIndex >= 0 && endIndex < Route.Count - 1)
                {
                    var endCity = Route[endIndex];
                    Route.RemoveAt(endIndex);
                    Route.Add(endCity);
                }
            }
        }

        private int SelectNextCity(int currentCity, double[,] distances, double[,] pheromones, double alpha, double beta)
        {
            var probabilities = new List<(int city, double probability)>();
            double sum = 0.0;

            for (int city = 0; city < _cityCount; city++)
            {
                if (_visited.Contains(city))
                    continue;

                var distance = distances[currentCity, city];
                if (distance == 0.0)
                    distance = 0.001;

                var pheromone = Math.Pow(pheromones[currentCity, city], alpha);
                var visibility = Math.Pow(1.0 / distance, beta);
                var probability = pheromone * visibility;

                probabilities.Add((city, probability));
                sum += probability;
            }

            if (probabilities.Count == 0)
                return 0;

            var randomValue = _random.NextDouble() * sum;
            double cumulative = 0.0;

            foreach (var (city, probability) in probabilities)
            {
                cumulative += probability;
                if (cumulative >= randomValue)
                {
                    return city;
                }
            }

            return probabilities.Last().city;
        }
    }
}
