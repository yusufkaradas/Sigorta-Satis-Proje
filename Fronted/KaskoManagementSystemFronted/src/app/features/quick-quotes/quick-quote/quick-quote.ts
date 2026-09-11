import {
  Component
} from '@angular/core';

import {
  RouterLink
} from '@angular/router';

@Component({
  selector: 'app-quick-quote',
  standalone: true,

  imports: [
    RouterLink
  ],

  templateUrl: './quick-quote.html',
  styleUrl: './quick-quote.scss'
})
export class QuickQuote {
}