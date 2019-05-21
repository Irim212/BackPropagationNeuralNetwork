
using System;
using System.Collections.Generic;
using System.Linq;

namespace BackpropagationNeuralNetwork
{
	internal static class IdHelper
	{
		private static int id = 0;

		public static void GetNextId(ref int value)
		{
			value = id++;
		}
	}

	internal static class RandomHelper
	{
		private static readonly Random random = new Random();

		public static double GetRandomMinusOneToOne()
		{
			return random.NextDouble() * 2 - 1;
		}

		public static int GetRandomInRange(int min, int max)
		{
			return random.Next(min, max);
		}
	}

	internal struct NeuronData
	{
		public int Id;
		public double Output;
		public double BiasWeight;
		public double BiasDiff;
		public double SignalError;
	}

	internal sealed class Neuron
	{
		private NeuronData neuronData;

		private Dictionary<int, Neuron> leftSideNeurons;
		private Dictionary<int, double> weights;
		private Dictionary<int, double> weightsDifferences;

		internal Neuron() // input layer neuron constructor
		{
			initializeNeuronData(true, null);
		}

		internal Neuron(IReadOnlyCollection<Neuron> attachedNeurons) // hidden/output layer neuron constructor
		{
			initializeNeuronData(false, attachedNeurons);
		}

		private int getNeuronId() => neuronData.Id;

		internal void activate() => neuronData.Output = 1d / (1d + Math.Exp(-neuronData.Output));

		internal void sumWeights() =>
			neuronData.Output = weights.Select(pair => pair.Value * leftSideNeurons[pair.Key].getOutput()).Sum() + neuronData.BiasWeight;

		private void initializeNeuronData(bool isInputLayerNeuron, IReadOnlyCollection<Neuron> attachedNeurons)
		{
			neuronData = new NeuronData();
			IdHelper.GetNextId(ref neuronData.Id);

			if (isInputLayerNeuron) 
				return;
			
			initializeDictionaries(attachedNeurons);
			neuronData.BiasWeight = RandomHelper.GetRandomMinusOneToOne();
			neuronData.BiasDiff = 0d;
		}

		private void initializeDictionaries(IReadOnlyCollection<Neuron> attachedNeurons)
		{
			leftSideNeurons = new Dictionary<int, Neuron>(attachedNeurons.Count);
			weights = new Dictionary<int, double>(attachedNeurons.Count);
			weightsDifferences = new Dictionary<int, double>(attachedNeurons.Count);

			foreach (Neuron neuron in attachedNeurons)
			{
				leftSideNeurons.Add(neuron.getNeuronId(), neuron);
				weights.Add(neuron.getNeuronId(), RandomHelper.GetRandomMinusOneToOne());
				weightsDifferences.Add(neuron.getNeuronId(), 0);
			}
		}

		internal void setOutput(double output) => neuronData.Output = output;

		internal double getWeight(Neuron neuron) => weights[neuron.getNeuronId()];

		internal double getSignalError() => neuronData.SignalError;

		internal double getOutput() => neuronData.Output;

		internal void setSignalError(double signalError) => neuronData.SignalError = signalError;

		internal void setBiasDiff(double biasDiff) => neuronData.BiasDiff = biasDiff;

		internal double getBiasDiff() => neuronData.BiasDiff;

		internal double getBiasWeight() => neuronData.BiasWeight;

		internal void setBiasWeight(double biasWeight) => neuronData.BiasWeight = biasWeight;

		internal void setWeightDiff(Neuron neuron, double weightDifference) => weightsDifferences[neuron.getNeuronId()] = weightDifference;

		internal double getWeightDiff(Neuron neuron) => weightsDifferences[neuron.getNeuronId()];

		internal void setWeight(Neuron neuron, double weight) => weights[neuron.getNeuronId()] = weight;
	}
}
