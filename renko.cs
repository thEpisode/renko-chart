// 
// Copyright (C) 2020, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.ComponentModel;
using NinjaTrader;
using NinjaTrader.Cbi;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
#endregion

namespace NinjaTrader.NinjaScript.BarsTypes
{
	public class MiRenkDavis : BarsType
	{
		// Tendencia alcista = 1
		// Tendencia indefinida = 0 (análisis de primer barra)
		// Tendencia bajista = -1
		private int trend;
		private			double			offset;
		private			double			renkoHigh;
		private			double			renkoLow;

		public override void ApplyDefaultBasePeriodValue(BarsPeriod period) {}

		public override void ApplyDefaultValue(BarsPeriod period)
		{
			period.Value = 2;
		}

		public override string ChartLabel(DateTime time) { return time.ToString("T", Core.Globals.GeneralOptions.CurrentCulture); }

		public override int GetInitialLookBackDays(BarsPeriod period, TradingHours tradingHours, int barsBack) { return 3; }

		public override double GetPercentComplete(Bars bars, DateTime now) { return 0; }

		public override bool IsRemoveLastBarSupported { get { return true; } }

		protected override void OnDataPoint(Bars bars, double open, double high, double low, double close, DateTime time, long volume, bool isBar, double bid, double ask)
		{
			double lastBarClose	= bars.GetClose(bars.Count - 1); // Trae el último precio de cierre

			if (SessionIterator == null)
				SessionIterator = new SessionIterator(bars);

			offset = bars.BarsPeriod.Value * bars.Instrument.MasterInstrument.TickSize; // Periocidad * tick (Tamaño de la caja)
			bool isNewSession = SessionIterator.IsNewSession(time, isBar);
			if (isNewSession)
				SessionIterator.GetNextSession(time, isBar);

			// Si es una gráfica nueva o una sesión nueva y está en un nuevo día
			if (bars.Count == 0 || bars.IsResetOnNewTradingDay && isNewSession)
			{
				// una sesión nueva y está en un nuevo día
				if (bars.Count > 0)
				{
					// Close out last bar in session and set open == close
					DateTime	lastBarTime		= bars.GetTime(bars.Count - 1); // Trae la última fecha del precio de cierre
					long		lastBarVolume	= bars.GetVolume(bars.Count - 1); // Trae el último volumen de precio de cierre
					RemoveLastBar(bars); // Elimina la última barra
					AddBar(bars, lastBarClose, lastBarClose, lastBarClose, lastBarClose, lastBarTime, lastBarVolume); // Crea una nueva barra con la información obtenida
				}

				renkoHigh	= close + offset; // Suma el valor de cierre y tamaño de la caja
				renkoLow	= close - offset; // Diferencia del valor de cierre y el tamaño de la caja

				// ¿Hay un nuevo precio negociado?
				isNewSession = SessionIterator.IsNewSession(time, isBar);
				if (isNewSession)
					SessionIterator.GetNextSession(time, isBar); // Entonces traiga el último precio

				// Pinte el último precio negociado
				AddBar(bars, close, close, close, close, time, volume);
				bars.LastPrice = close;

				return;
			}

			double		barOpen		= bars.GetOpen(bars.Count - 1);   // Obtiene el valor de apertura de la última barra
			double		barHigh		= bars.GetHigh(bars.Count - 1);   // Obtiene el valor más alto negociado de la última barra
			double		barLow		= bars.GetLow(bars.Count - 1);    // Obtiene el valor más bajo negociado de la última barra
			long		barVolume	= bars.GetVolume(bars.Count - 1); // Obtiene el volumen de la última barra
			DateTime	barTime		= bars.GetTime(bars.Count - 1);   // Obtiene la fecha de la última barra

			// renkoHigh == 0 || renkoLow == 0
			// ApproxCompare: Compares two double or float values for equality or being greater than / less than the compared to value.
			if (renkoHigh.ApproxCompare(0.0) == 0 || renkoLow.ApproxCompare(0.0) == 0)
			{
				if (bars.Count == 1)
				{
					renkoHigh	= barOpen + offset; // suma = valor de apertura de la última barra + el tamaño de la caja
					renkoLow	= barOpen - offset; // diferencia = valor de apertura de la última barra - el tamaño de la caja
				}
				// Penultimo valor de cierre es mayor al penultimo valor de apertura?
				else if (bars.GetClose(bars.Count - 2) > bars.GetOpen(bars.Count - 2))
				{
					renkoHigh	= bars.GetClose(bars.Count - 2) + offset;		// Suma tamaño de la caja + el penultimo valor de cierre
					renkoLow	= bars.GetClose(bars.Count - 2) - offset * 2;	// Resta el doble del tamaño de la caja - el penultimo valor de cierre
				}
				else
				{
					renkoHigh	= bars.GetClose(bars.Count - 2) + offset * 2;	// Suma el doble tamaño de la caja + el penultimo valor de cierre
					renkoLow	= bars.GetClose(bars.Count - 2) - offset;		// Resta el tamaño de la caja - el penultimo valor de cierre
				}
			}

			bool isRail2Rail = false;
			// Hay cambio de tendencia hacia bajista?
			if(close <= (renkoHigh)){
				// Elimina la barra de Update
				RemoveLastBar(bars);

				// Agrega la nueva barra con los nuevos valores
				renkoLow	= renkoHigh - 2.0 * offset; // RenkoHigh - el doble del tamaño de la caja
				renkoHigh	= renkoHigh + offset;		// Renkohigh + el tamaño de la caja
				// Agrega barra alcista
				//AddBar(bars, _renkoLow - offset, Math.Max(_renkoLow - offset, _renkoLow), Math.Min(_renkoLow - offset, _renkoLow), _renkoLow, barTime, barVolume);
				//AddBar(bars, _renkoHigh + offset, Math.Max(_renkoHigh + offset, _renkoHigh), Math.Min(_renkoHigh + offset, _renkoHigh), _renkoHigh, barTime, barVolume);
				//AddBar(Bars bars, double open, double high, double low, double close, DateTime time, long volume)
				
				//Barra de una gráfica bajista
				AddBar(bars, renkoLow + offset, Math.Max(renkoLow + offset, renkoLow), Math.Min(renkoLow + offset, renkoLow), renkoLow, barTime, barVolume);
								
				isRail2Rail = true;
			}
			//bool isRail2Rail = false;
			// Hay cambio de tendencia hacia alcista?
			if(close >= (renkoLow)) {
				// Elimina la barra la barra de Update
				RemoveLastBar(bars);
				// Agrega la nueva barra con los nuevos valores
				// Original
				//AddBar(bars, renkoLow + offset, Math.Max(renkoLow + offset, renkoLow), Math.Min(renkoLow + offset, renkoLow), renkoLow, barTime, barVolume);
				// Bajista
				renkoHigh	= renkoLow + 2.0 * offset;	// RenkoLow - el doble del tamaño de la caja
				renkoLow	= renkoLow - offset;		// RenkoLow - el tamaño de la caja
				//AddBar(bars, _renkoHigh + offset, Math.Max(_renkoHigh + offset, _renkoHigh), Math.Min(_renkoHigh + offset, _renkoHigh), _renkoHigh, barTime, barVolume);
				//AddBar(bars, _renkoLow - offset, Math.Max(_renkoLow - offset, _renkoLow), Math.Min(_renkoLow - offset, _renkoLow), _renkoLow, barTime, barVolume);
				
				//Barra de gráfica alcista
				AddBar(bars, renkoHigh - offset, Math.Max(renkoHigh - offset, renkoHigh), Math.Min(renkoHigh - offset, renkoHigh), renkoHigh, barTime, barVolume);
				
				isRail2Rail = true;
			}
			// el precio de cierre es mayor renkohigh?
      		// [DETECTA COMPORTAMIENTO ALCISTA]
			if (close.ApproxCompare(renkoHigh) >= 0)
			{
				
				/* if (trend != 0 && trend != 1) {
					// Elimina la barra de Update
					RemoveLastBar(bars);

					// Agrega la nueva barra con los nuevos valores
					var _renkoLow	= renkoHigh - 1.0 * offset; // RenkoHigh - el doble del tamaño de la caja
					var _renkoHigh	= renkoHigh + offset;		// Renkohigh + el tamaño de la caja
					// Agrega barra alcista
					AddBar(bars, _renkoLow - offset, Math.Max(_renkoLow - offset, _renkoLow), Math.Min(_renkoLow - offset, _renkoLow), _renkoLow, barTime, barVolume);
					
					isRail2Rail = true;
          		} */
				// (1) Obtiene el valor mayor entre renkoHigh y, renkoHigh - tamaño de la caja
				// (2) Si el valor de x es igual a y entonces retorna 0
				//		Si el valor de x es mayor a y entonces retorna 1
				//		Si el valor de x es menor a y entonces retorna -1
				if (barOpen.ApproxCompare(renkoHigh - offset) != 0 // valor de apertura de la (última barra - el tamño de la caja) es mayor o menor a barOpen?
					|| barHigh.ApproxCompare(Math.Max(renkoHigh - offset, renkoHigh)) != 0 // Es barHigh mayor o menor a (1)?
					|| barLow.ApproxCompare(Math.Min(renkoHigh - offset, renkoHigh)) != 0)// Es barLow mayor o menor a (1)?
				{
          			// No hubo cambio de tendencia
					if(!isRail2Rail)
					{
						// Elimina la última barra de Update
						RemoveLastBar(bars);
					}
					
					// Agrega una barra nueva con los nuevos valores
					// Alcista
					AddBar(bars, renkoHigh - offset, Math.Max(renkoHigh - offset, renkoHigh), Math.Min(renkoHigh - offset, renkoHigh), renkoHigh, barTime, barVolume);
				}

				renkoLow	= renkoHigh - 2.0 * offset; // RenkoHigh - el doble del tamaño de la caja
				renkoHigh	= renkoHigh + offset;		// Renkohigh + el tamaño de la caja

				// ¿Hay un nuevo valor negociado?
				isNewSession = SessionIterator.IsNewSession(time, isBar);
				if (isNewSession)
					SessionIterator.GetNextSession(time, isBar); // Obtiene el último valor negociado

				// Agrega barras vacías para llenar el gap si el precio salta
				while (close.ApproxCompare(renkoHigh) >= 0)
				{
					AddBar(bars, renkoHigh - offset, Math.Max(renkoHigh - offset, renkoHigh), Math.Min(renkoHigh - offset, renkoHigh), renkoHigh, time, 0);
					renkoLow	= renkoHigh - 2.0 * offset;
					renkoHigh	= renkoHigh + offset;
				}

				// Agrega la barra final parcial
				AddBar(bars, renkoHigh - offset, Math.Max(renkoHigh - offset, close), Math.Min(renkoHigh - offset, close), close, time, volume);
				trend = 1;
			}
			// el precio de cierre es menor o igual a renkohigh?
      		// El precio de cierre es menor o igual al renkolow
			// [DETECTA COMPORTAMIENTO BAJISTA]
			else if (close.ApproxCompare(renkoLow) <= 0)
			{
				
				/* if (trend != 0 && trend != -1) {
					// Elimina la barra la barra de Update
					RemoveLastBar(bars);
					// Agrega la nueva barra con los nuevos valores
					// Original
					//AddBar(bars, renkoLow + offset, Math.Max(renkoLow + offset, renkoLow), Math.Min(renkoLow + offset, renkoLow), renkoLow, barTime, barVolume);
					// Bajista
					var _renkoHigh	= renkoLow + 1.0 * offset;	// RenkoLow - el doble del tamaño de la caja
					var _renkoLow	= renkoLow - offset;		// RenkoLow - el tamaño de la caja
					AddBar(bars, _renkoHigh + offset, Math.Max(_renkoHigh + offset, _renkoHigh), Math.Min(_renkoHigh + offset, _renkoHigh), _renkoHigh, barTime, barVolume);
					isRail2Rail = true;
				} */
				// (1) Obtiene el valor mayor entre renkoLow y, renkoLow + tamaño de la caja
				// (2) Si el valor de x es igual a y entonces retorna 0
				//		Si el valor de x es mayor a y entonces retorna 1
				//		Si el valor de x es menor a y entonces retorna -1
				if (barOpen.ApproxCompare(renkoLow + offset) != 0 // Valor de apertura de (renkolow + el tamaño de la caja) es mayor o menor a barOpen?
					|| barHigh.ApproxCompare(Math.Max(renkoLow + offset, renkoLow)) != 0 // Es barHigh mayor o menor a (1)?
					|| barLow.ApproxCompare(Math.Min(renkoLow + offset, renkoLow)) != 0)  // Es barlow mayor o menor a (1)?
				{
					// TODO: Validar si la condición cambia, si si, entonces no elimine la última barra
					if(!isRail2Rail){
						// Elimine la barra de Update
						RemoveLastBar(bars);
					}
					
					// Agrega la nueva barra con los nuevos valores
					// AddBar(Bars bars, double open, double high, double low, double close, DateTime time, long volume)
					//Bajista
					AddBar(bars, renkoLow + offset, Math.Max(renkoLow + offset, renkoLow), Math.Min(renkoLow + offset, renkoLow), renkoLow, barTime, barVolume);
				}

				renkoHigh	= renkoLow + 2.0 * offset;	// RenkoLow - el doble del tamaño de la caja
				renkoLow	= renkoLow - offset;		// RenkoLow - el tamaño de la caja

				// ¿Hay un nuevo valor negociado?
				isNewSession = SessionIterator.IsNewSession(time, isBar);
				if (isNewSession)
					SessionIterator.GetNextSession(time, isBar);  // Obtiene el último valor negociado

				// Agrega barras vacías para llenar el gap si el precio salta
				while (close.ApproxCompare(renkoLow) <= 0)
				{
					AddBar(bars, renkoLow + offset, Math.Max(renkoLow + offset, renkoLow), Math.Min(renkoLow + offset, renkoLow), renkoLow, time, 0);
					renkoHigh	= renkoLow + 2.0 * offset;
					renkoLow	= renkoLow - offset;
				}

				// Agrega la barra final parcial
				AddBar(bars, renkoLow + offset, Math.Max(renkoLow + offset, close), Math.Min(renkoLow + offset, close), close, time, volume);
				trend = -1;
			}
			// El precio de cierre mayor al renkolow
			else 
			{
				// Actualiza la barra
				UpdateBar(bars, close, close, close, time, volume);
			}

			// El último precio el valor de cierre
			bars.LastPrice	= close;
		}

		// Es un evento que se ejecuta tipo One-Time cuando va de histórico hacia real-time
		// Los estados pueden ser setup, processing data, to termination
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name							= "RenkoDavis";
				BarsPeriod				= new BarsPeriod {BarsPeriodType = (BarsPeriodType) 2018, BarsPeriodTypeName = "UniRenko (2018)", Value = 1};
				BuiltFrom					= BarsPeriodType.Tick;
				DaysToLoad				= 3;
				DefaultChartStyle	= Gui.Chart.ChartStyleType.OpenClose;
				IsIntraday				= true;
				IsTimeBased				= false;
			}
			else if (State == State.Configure)
			{
				Name				= string.Format(Core.Globals.GeneralOptions.CurrentCulture, Custom.Resource.DataBarsTypeRenko, BarsPeriod.Value);

				Properties.Remove(Properties.Find("BaseBarsPeriodType",			true));
				Properties.Remove(Properties.Find("BaseBarsPeriodValue",		true));
				Properties.Remove(Properties.Find("PointAndFigurePriceType",	true));
				Properties.Remove(Properties.Find("ReversalType",				true));
				Properties.Remove(Properties.Find("Value2",						true));

				SetPropertyName("Value", Custom.Resource.NinjaScriptBarsTypeRenkoBrickSize);
			}
		}
	}
}
